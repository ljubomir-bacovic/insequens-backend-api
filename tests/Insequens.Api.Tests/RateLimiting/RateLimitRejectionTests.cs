using System.Text;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Insequens.Api.Tests.RateLimiting;

public class RateLimitRejectionTests
{
    [Fact]
    public async Task WriteAsync_WhenRejected_LogsWarningWithHashedPartitionKey()
    {
        var collector = new FakeLogCollector();
        var context = CreateHttpContext(collector);
        const string partitionKey = "ip:192.0.2.55";

        await RateLimitRejection.WriteAsync(context, TimeSpan.FromSeconds(30), partitionKey, CancellationToken.None);

        var record = collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Warning);
        record.Message.Should().Contain(RateLimitPartitionKey.Hash(partitionKey));
        record.Message.Should().NotContain("192.0.2.55");
    }

    [Fact]
    public async Task WriteAsync_WithRetryAfter_Writes429ProblemDetailsAndRoundsRetryAfterUp()
    {
        var context = CreateHttpContext(new FakeLogCollector());

        await RateLimitRejection.WriteAsync(context, TimeSpan.FromSeconds(12.2), "user:1", CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.ContentType.Should().Be("application/problem+json");
        context.Response.Headers.RetryAfter.ToString().Should().Be("13");
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(429);
        body.RootElement.GetProperty("type").GetString().Should().Be("urn:insequens:error:rate-limited");
    }

    [Fact]
    public async Task WriteAsync_WithoutRetryAfter_OmitsHeader()
    {
        var context = CreateHttpContext(new FakeLogCollector());

        await RateLimitRejection.WriteAsync(context, retryAfter: null, "user:1", CancellationToken.None);

        context.Response.Headers.ContainsKey("Retry-After").Should().BeFalse();
    }

    [Fact]
    public void Hash_ForSameKey_IsStableAndShort()
    {
        var hash = RateLimitPartitionKey.Hash("email:USER@EXAMPLE.COM");

        hash.Should().HaveLength(16).And.Be(RateLimitPartitionKey.Hash("email:USER@EXAMPLE.COM"));
        hash.Should().NotBe(RateLimitPartitionKey.Hash("email:OTHER@EXAMPLE.COM"));
    }

    private static DefaultHttpContext CreateHttpContext(FakeLogCollector collector)
    {
        var services = new ServiceCollection()
            .AddLogging(logging => logging.AddProvider(new FakeLoggerProvider(collector)))
            .AddProblemDetails()
            .BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = services,
            Request = { Path = "/v1/Auth/login" },
            Response = { Body = new MemoryStream() },
        };
    }
}
