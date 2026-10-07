using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Insequens.Infrastructure.DataAccess.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Insequens.Infrastructure.Tests.Identity;

public class TokenServiceTests
{
    private const string Secret = "token-service-test-secret-0123456789";
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _timeProvider = new(Now);

    [Fact]
    public void CreateAccessToken_WithUserAndName_WritesV1ClaimNamesAndLifetime()
    {
        var userId = Guid.NewGuid();

        var accessToken = CreateService().CreateAccessToken(userId, [new Claim(ClaimTypes.Name, "user@example.com")]);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Value);
        token.GetClaim(JwtRegisteredClaimNames.NameId).Value.Should().Be(userId.ToString());
        token.GetClaim(JwtRegisteredClaimNames.UniqueName).Value.Should().Be("user@example.com");
        token.GetClaim(JwtRegisteredClaimNames.Jti).Value.Should().NotBeNullOrEmpty();
        token.IssuedAt.Should().Be(Now.UtcDateTime);
        token.ValidTo.Should().Be(Now.AddMinutes(15).UtcDateTime);
        accessToken.ExpiresAt.Should().Be(Now.AddMinutes(15));
    }

    [Fact]
    public void CreateAccessToken_WithOtherClaim_KeepsItsType()
    {
        var accessToken = CreateService().CreateAccessToken(Guid.NewGuid(), [new Claim("role", "Admin")]);

        new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Value).GetClaim("role").Value.Should().Be("Admin");
    }

    [Fact]
    public void CreateRefreshToken_ReturnsRandom64ByteValueExpiringAfterLifetime()
    {
        var service = CreateService();

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Convert.FromBase64String(first.Value).Should().HaveCount(64);
        first.Value.Should().NotBe(second.Value);
        first.ExpiresAt.Should().Be(Now.AddDays(7));
    }

    [Fact]
    public async Task ValidateExpiredAccessTokenAsync_WithExpiredOwnToken_ReturnsUserId()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var accessToken = service.CreateAccessToken(userId, []);
        _timeProvider.Advance(TimeSpan.FromDays(1));

        var result = await service.ValidateExpiredAccessTokenAsync(accessToken.Value);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    [InlineData("eyJhbGciOiJub25lIn0.eyJuYW1laWQiOiJ4In0.")]
    public async Task ValidateExpiredAccessTokenAsync_WithMalformedToken_ReturnsInvalidWithoutThrowing(string token)
    {
        var result = await CreateService().ValidateExpiredAccessTokenAsync(token);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("another-signing-secret-0123456789012", "issuer", "audience")]
    [InlineData(Secret, "other-issuer", "audience")]
    [InlineData(Secret, "issuer", "other-audience")]
    public async Task ValidateExpiredAccessTokenAsync_WithForeignToken_ReturnsInvalid(string secret, string issuer, string audience)
    {
        var token = CreateToken(secret, issuer, audience, new Claim(JwtRegisteredClaimNames.NameId, Guid.NewGuid().ToString()));

        var result = await CreateService().ValidateExpiredAccessTokenAsync(token);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateExpiredAccessTokenAsync_WithoutGuidUserId_ReturnsInvalid()
    {
        var token = CreateToken(Secret, "issuer", "audience", new Claim(JwtRegisteredClaimNames.NameId, "not-a-guid"));

        var result = await CreateService().ValidateExpiredAccessTokenAsync(token);

        result.IsValid.Should().BeFalse();
    }

    private TokenService CreateService()
    {
        var options = Options.Create(new JwtOptions { Issuer = "issuer", Audience = "audience", Key = Secret });

        return new TokenService(new JwtKeyRing(options, _timeProvider), options, _timeProvider, NullLogger<TokenService>.Instance);
    }

    private static string CreateToken(string secret, string issuer, string audience, Claim claim) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([claim]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256),
        });
}
