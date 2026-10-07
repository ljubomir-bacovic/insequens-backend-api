using System.Globalization;
using System.Net;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Insequens.Api.Tests.Auth;

public class SigningKeyRotationTests
{
    private const string KeyASecret = "rotation-test-key-a-0123456789012345";
    private const string KeyBSecret = "rotation-test-key-b-0123456789012345";

    [Fact]
    public async Task Token_SignedWithKeyA_ValidatesAfterKeyBBecomesActive_AndNotAfterKeyAIsRemoved()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        string tokenSignedWithA;

        await using (var keyAOnly = new InsequensApiFactory(Keys(("a", KeyASecret, now.AddDays(-30)))))
        {
            tokenSignedWithA = keyAOnly.CreateAccessToken(userId);
        }

        await using var keyAAndB = new InsequensApiFactory(
            Keys(("a", KeyASecret, now.AddDays(-30)), ("b", KeyBSecret, now.AddMinutes(-1))));
        await using var keyBOnly = new InsequensApiFactory(Keys(("b", KeyBSecret, now.AddMinutes(-1))));

        var whileBothConfigured = await GetToDoItemsAsync(keyAAndB, tokenSignedWithA);
        var afterARemoved = await GetToDoItemsAsync(keyBOnly, tokenSignedWithA);

        new JsonWebTokenHandler().ReadJsonWebToken(tokenSignedWithA).Kid.Should().Be("a");
        new JsonWebTokenHandler().ReadJsonWebToken(keyAAndB.CreateAccessToken(userId)).Kid.Should().Be("b");
        whileBothConfigured.Should().Be(HttpStatusCode.OK);
        afterARemoved.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_WhenNextKeyIsNotActiveYet_IsSignedWithCurrentKey()
    {
        var now = DateTimeOffset.UtcNow;
        await using var factory = new InsequensApiFactory(
            Keys(("a", KeyASecret, now.AddDays(-30)), ("b", KeyBSecret, now.AddDays(1))));

        var token = factory.CreateAccessToken(Guid.NewGuid());

        new JsonWebTokenHandler().ReadJsonWebToken(token).Kid.Should().Be("a");
    }

    [Fact]
    public async Task Token_SignedWithSingleKey_HasNoKeyId_AndValidates()
    {
        await using var factory = new InsequensApiFactory();

        var token = factory.CreateAccessToken(Guid.NewGuid());

        new JsonWebTokenHandler().ReadJsonWebToken(token).Kid.Should().BeNullOrEmpty();
        (await GetToDoItemsAsync(factory, token)).Should().Be(HttpStatusCode.OK);
    }

    private static Dictionary<string, string?> Keys(params (string Id, string Secret, DateTimeOffset ActiveFrom)[] keys)
    {
        var settings = new Dictionary<string, string?> { ["Jwt:Key"] = string.Empty };

        for (var index = 0; index < keys.Length; index++)
        {
            settings[$"Jwt:Keys:{index}:Id"] = keys[index].Id;
            settings[$"Jwt:Keys:{index}:Secret"] = keys[index].Secret;
            settings[$"Jwt:Keys:{index}:ActiveFrom"] = keys[index].ActiveFrom.ToString("O", CultureInfo.InvariantCulture);
        }

        return settings;
    }

    private static async Task<HttpStatusCode> GetToDoItemsAsync(InsequensApiFactory factory, string accessToken)
    {
        using var client = factory.CreateHttpsClient();
        client.UseBearer(accessToken);

        return (await client.GetAsync("/v1/ToDoItem")).StatusCode;
    }
}
