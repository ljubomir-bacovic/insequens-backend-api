using FluentAssertions;
using Insequens.Infrastructure.DataAccess.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;

namespace Insequens.Infrastructure.Tests.Identity;

public class JwtKeyRingTests
{
    private const string SecretA = "key-a-signing-secret-0123456789012345";
    private const string SecretB = "key-b-signing-secret-0123456789012345";
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetSigningCredentials_WithSingleKey_UsesItWithoutKeyId()
    {
        var keyRing = CreateKeyRing(new JwtOptions { Key = SecretA }, new FakeTimeProvider(Now));

        var credentials = keyRing.GetSigningCredentials();

        credentials.Algorithm.Should().Be(SecurityAlgorithms.HmacSha256);
        credentials.Key.KeyId.Should().BeNull();
    }

    [Fact]
    public void GetSigningCredentials_WithRotationKeys_UsesNewestActiveKey()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var keyRing = CreateKeyRing(RotationOptions(), timeProvider);

        var beforeB = keyRing.GetSigningCredentials();
        timeProvider.Advance(TimeSpan.FromDays(1));
        var afterB = keyRing.GetSigningCredentials();

        beforeB.Key.KeyId.Should().Be("a");
        afterB.Key.KeyId.Should().Be("b");
    }

    [Fact]
    public void GetSigningCredentials_WhenNoKeyIsActive_Throws()
    {
        var keyRing = CreateKeyRing(RotationOptions(), new FakeTimeProvider(Now.AddDays(-30)));

        var action = () => keyRing.GetSigningCredentials();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateValidationParameters_IncludesEveryKeyAndStrictSettings()
    {
        var keyRing = CreateKeyRing(RotationOptions(), new FakeTimeProvider(Now));

        var parameters = keyRing.CreateValidationParameters(validateLifetime: true);

        parameters.IssuerSigningKeys.Select(key => key.KeyId).Should().Equal("a", "b");
        parameters.ValidIssuer.Should().Be("issuer");
        parameters.ValidAudience.Should().Be("audience");
        parameters.ValidAlgorithms.Should().Equal(SecurityAlgorithms.HmacSha256);
        parameters.ValidateLifetime.Should().BeTrue();
        parameters.ClockSkew.Should().Be(TimeSpan.Zero);
        keyRing.CreateValidationParameters(validateLifetime: false).ValidateLifetime.Should().BeFalse();
    }

    private static JwtOptions RotationOptions() => new()
    {
        Keys =
        [
            new JwtSigningKey { Id = "a", Secret = SecretA, ActiveFrom = Now.AddDays(-7) },
            new JwtSigningKey { Id = "b", Secret = SecretB, ActiveFrom = Now.AddDays(1) },
        ],
    };

    private static JwtKeyRing CreateKeyRing(JwtOptions options, TimeProvider timeProvider) =>
        new(Options.Create(options with { Issuer = "issuer", Audience = "audience" }), timeProvider);
}
