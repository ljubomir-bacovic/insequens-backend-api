using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Tests.Validators.Auth;

public class ForgotPasswordValidatorTests
{
    private readonly ForgotPasswordValidator _validator = new();

    [Fact]
    public void Validate_WithValidEmail_ReturnsNoErrors()
    {
        _validator.Validate(new ForgotPasswordCommand("user@example.com")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_ReturnsError(string email)
    {
        _validator.Validate(new ForgotPasswordCommand(email)).Errors.Should().Contain(error => error.PropertyName == "Email");
    }
}
