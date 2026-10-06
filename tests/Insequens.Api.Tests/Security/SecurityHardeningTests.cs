using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Insequens.Api.Tests.Security;

public class SecurityHardeningTests
{
    [Fact]
    public async Task Response_On200_HasSecurityHeaders()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/warmup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().Equal("no-referrer");
        response.Headers.GetValues("Permissions-Policy").Single().Should().Contain("camera=()");
    }

    [Fact]
    public async Task Response_OnError_HasSecurityHeaders()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/v1/ToDoItem");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
    }

    [Fact]
    public async Task AuthResponse_HasNoStoreCacheControl()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email = "user@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task ControllerWithoutAuthorize_WithoutToken_Returns401()
    {
        await using var factory = new InsequensApiFactory(configureServices: services =>
            services.AddControllers().AddApplicationPart(typeof(UnprotectedTestController).Assembly));
        using var client = factory.CreateHttpsClient();

        var anonymousResponse = await client.GetAsync("/test/unprotected");
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));
        var authenticatedResponse = await client.GetAsync("/test/unprotected");

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        authenticatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiDocument_InDevelopment_IsAnonymous()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HttpsResponse_OutsideDevelopment_HasHsts()
    {
        await using var factory = new InsequensApiFactory(InsequensApiFactory.ProductionSettings, environment: "Production");
        using var client = factory.CreateHttpsClient("api.insequens.test");

        var response = await client.GetAsync("/warmup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Strict-Transport-Security").Single().Should().StartWith("max-age=");
    }

    [Fact]
    public async Task Request_OutsideDevelopment_ForUnlistedHost_Returns400()
    {
        await using var factory = new InsequensApiFactory(InsequensApiFactory.ProductionSettings, environment: "Production");
        using var client = factory.CreateHttpsClient("unlisted.insequens.test");

        var response = await client.GetAsync("/warmup");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void Kestrel_MaxRequestBodySize_IsOneMegabyte()
    {
        using var factory = new InsequensApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        options.Limits.MaxRequestBodySize.Should().Be(1024 * 1024);
    }
}
