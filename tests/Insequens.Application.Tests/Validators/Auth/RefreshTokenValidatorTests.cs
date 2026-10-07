using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Tests.Validators.Auth;

public class RefreshTokenValidatorTests
{
    private readonly RefreshTokenValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        _validator.Validate(new RefreshTokenCommand("access", "refresh")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "refresh", "Token")]
    [InlineData("access", "", "RefreshToken")]
    public void Validate_WithEmptyField_ReturnsError(string token, string refreshToken, string invalidField)
    {
        _validator.Validate(new RefreshTokenCommand(token, refreshToken)).Errors.Should().ContainSingle(error => error.PropertyName == invalidField);
    }
}
