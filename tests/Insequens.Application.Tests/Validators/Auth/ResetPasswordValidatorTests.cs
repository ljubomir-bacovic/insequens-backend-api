using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Tests.Validators.Auth;

public class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        _validator.Validate(new ResetPasswordCommand("user@example.com", "token", "N3w-Passw0rd!")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-an-email", "token", "N3w-Passw0rd!", "Email")]
    [InlineData("user@example.com", "", "N3w-Passw0rd!", "Token")]
    [InlineData("user@example.com", "token", "weak", "NewPassword")]
    public void Validate_WithInvalidField_ReturnsError(string email, string token, string newPassword, string invalidField)
    {
        var result = _validator.Validate(new ResetPasswordCommand(email, token, newPassword));

        result.Errors.Should().Contain(error => error.PropertyName == invalidField);
    }
}
