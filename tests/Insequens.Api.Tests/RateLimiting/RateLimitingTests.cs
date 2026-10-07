using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.RateLimiting;

public class RateLimitingTests
{
    [Fact]
    public async Task Login_EleventhAttemptWithinAMinute_Returns429WithRetryAfter()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            (await client.LoginAsync($"user{attempt}@example.com", Password))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"attempt {attempt} is within the limit");
        }

        var response = await client.LoginAsync();

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter!.Delta.Should().BePositive();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().Should().Be(429);
    }

    [Fact]
    public async Task Login_SameEmailFromDifferentClients_Returns429AfterLimit()
    {
        await using var factory = new InsequensApiFactory(
            new Dictionary<string, string?> { ["RateLimiting:Auth:PermitLimit"] = "2" },
            configureServices: services => services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>());
        using var client = factory.CreateHttpsClient();

        var responses = new List<HttpStatusCode>();
        foreach (var clientIp in new[] { "192.0.2.1", "192.0.2.2", "192.0.2.3" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/Auth/login")
            {
                Content = JsonContent.Create(new { Email, Password }),
            };
            request.Headers.Add(TestClientIpStartupFilter.HeaderName, clientIp);
            responses.Add((await client.SendAsync(request)).StatusCode);
        }

        var otherEmailResponse = await client.LoginAsync("other@example.com", Password);

        responses.Should().Equal(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests);
        otherEmailResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the limit is per email address");
    }

    [Fact]
    public async Task WriteEndpoint_OverTokenBucket_Returns429()
    {
        await using var factory = new InsequensApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Write:TokenLimit"] = "2",
            ["RateLimiting:Write:TokensPerPeriod"] = "1",
            ["RateLimiting:Write:ReplenishmentPeriod"] = "01:00:00",
        });
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var responses = new List<HttpStatusCode>();
        for (var request = 0; request < 3; request++)
        {
            responses.Add((await client.PostAsJsonAsync("/v1/ToDoItem", new { Name = "Task", Priority = 0 })).StatusCode);
        }

        var readResponse = await client.GetAsync("/v1/ToDoItem");

        responses.Should().Equal(HttpStatusCode.Created, HttpStatusCode.Created, HttpStatusCode.TooManyRequests);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK, "reads are not counted by the write policy");
    }

    [Fact]
    public async Task GlobalLimit_Exceeded_Returns429()
    {
        await using var factory = new InsequensApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Global:PermitLimit"] = "2",
        });
        using var client = factory.CreateHttpsClient();

        await client.GetAsync("/warmup");
        await client.GetAsync("/warmup");
        var response = await client.GetAsync("/warmup");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void ForwardedHeaders_TrustOnlyConfiguredProxies()
    {
        using var factory = new InsequensApiFactory(new Dictionary<string, string?>
        {
            ["ReverseProxy:KnownProxies:0"] = "192.0.2.10",
            ["ReverseProxy:KnownNetworks:0"] = "198.51.100.0/24",
        });

        var options = factory.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        options.KnownProxies.Select(proxy => proxy.ToString()).Should().Equal("192.0.2.10");
        options.KnownIPNetworks.Select(network => network.ToString()).Should().Equal("198.51.100.0/24");
    }

    [Fact]
    public void ForwardedHeaders_WithoutConfiguredProxies_TrustNothing()
    {
        using var factory = new InsequensApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.KnownProxies.Should().BeEmpty();
        options.KnownIPNetworks.Should().BeEmpty();
    }
}
