using System.Net;
using FluentAssertions;
using Insequens.Api.Tests.Support;

namespace Insequens.Api.Tests.Auth;

public class ProtectedEndpointTests
{
    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/v1/ToDoItem");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync("/v1/ToDoItem");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        await using var factory = new InsequensApiFactory(startTime: DateTimeOffset.UtcNow.AddHours(-1));
        using var client = factory.CreateHttpsClient();
        var expiredToken = factory.CreateAccessToken(Guid.NewGuid());
        factory.Clock.SetUtcNow(DateTimeOffset.UtcNow);
        client.UseBearer(expiredToken);

        var response = await client.GetAsync("/v1/ToDoItem");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
