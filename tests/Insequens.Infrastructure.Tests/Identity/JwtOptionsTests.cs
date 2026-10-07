using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Insequens.Infrastructure.DataAccess.Identity;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Infrastructure.Tests.Identity;

public class JwtOptionsTests
{
    private const string Secret = "a-signing-secret-of-at-least-32-chars";
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_WithSingleKey_ReturnsNoErrors()
    {
        Validate(ValidOptions()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void Validate_WithMissingOrShortKey_NamesJwtKey(string? key)
    {
        Validate(ValidOptions() with { Key = key }).Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Jwt:Key");
    }

    [Fact]
    public void Validate_WithKeyAndKeys_ReturnsError()
    {
        var options = ValidOptions() with { Keys = [new JwtSigningKey { Id = "a", Secret = Secret }] };

        Validate(options).Should().ContainSingle(result => result.ErrorMessage!.Contains("either Jwt:Key or Jwt:Keys"));
    }

    [Fact]
    public void Validate_WithInvalidRotationKeys_ReportsEachProblem()
    {
        var options = ValidOptions() with
        {
            Key = null,
            Keys =
            [
                new JwtSigningKey { Id = "", Secret = Secret },
                new JwtSigningKey { Id = "b", Secret = "short" },
                new JwtSigningKey { Id = "b", Secret = Secret },
            ],
        };

        Validate(options).Select(result => result.ErrorMessage).Should().BeEquivalentTo(
            "Jwt:Keys:0:Id is required.",
            "Jwt:Keys:1:Secret is required and must be at least 32 characters.",
            "Jwt:Keys must have unique Ids. Duplicated: b.");
    }

    [Fact]
    public void Validate_WithMissingIssuerAndAudience_ReturnsErrors()
    {
        var results = Validate(ValidOptions() with { Issuer = "", Audience = "" });

        results.SelectMany(result => result.MemberNames).Should().BeEquivalentTo("Issuer", "Audience");
    }

    [Fact]
    public void ActiveKeyValidator_WithOnlyFutureKeys_Fails()
    {
        var options = ValidOptions() with
        {
            Key = null,
            Keys = [new JwtSigningKey { Id = "a", Secret = Secret, ActiveFrom = Now.AddMinutes(1) }],
        };

        var result = new JwtActiveKeyValidator(new FakeTimeProvider(Now)).Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ActiveFrom");
    }

    [Fact]
    public void ActiveKeyValidator_WithOneActiveKey_Succeeds()
    {
        var options = ValidOptions() with
        {
            Key = null,
            Keys =
            [
                new JwtSigningKey { Id = "a", Secret = Secret, ActiveFrom = Now },
                new JwtSigningKey { Id = "b", Secret = Secret, ActiveFrom = Now.AddDays(1) },
            ],
        };

        new JwtActiveKeyValidator(new FakeTimeProvider(Now)).Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void ActiveKeyValidator_WithSingleKey_Skips()
    {
        new JwtActiveKeyValidator(new FakeTimeProvider(Now)).Validate(null, ValidOptions()).Skipped.Should().BeTrue();
    }

    private static JwtOptions ValidOptions() => new()
    {
        Issuer = "https://localhost:7269",
        Audience = "http://localhost:3000",
        Key = Secret,
    };

    private static List<ValidationResult> Validate(JwtOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        return results;
    }
}
