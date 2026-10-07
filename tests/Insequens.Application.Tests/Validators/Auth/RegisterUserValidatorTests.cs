using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Tests.Validators.Auth;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        _validator.Validate(new RegisterUserCommand("user@example.com", "Valid-Passw0rd")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Email is required.")]
    [InlineData("not-an-email", "Email must be a valid email address.")]
    public void Validate_WithInvalidEmail_ReturnsError(string email, string expectedMessage)
    {
        var result = _validator.Validate(new RegisterUserCommand(email, "Valid-Passw0rd"));

        result.Errors.Should().ContainSingle(error => error.PropertyName == "Email" && error.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_WithEmailLongerThan256Characters_ReturnsError()
    {
        var result = _validator.Validate(new RegisterUserCommand(new string('a', 250) + "@example.com", "Valid-Passw0rd"));

        result.Errors.Should().Contain(error => error.PropertyName == "Email" && error.ErrorMessage == "Email must not exceed 256 characters.");
    }

    [Theory]
    [InlineData("", "Password is required.")]
    [InlineData("Sh0rt!", "Password must be between 8 and 128 characters.")]
    [InlineData("No-Digits-Here", "Password must contain a digit.")]
    [InlineData("NO-LOWER-1", "Password must contain a lowercase letter.")]
    [InlineData("no-upper-1", "Password must contain an uppercase letter.")]
    [InlineData("NoSymbol123", "Password must contain a character that is not a letter or digit.")]
    public void Validate_WithPasswordOutsidePolicy_ReturnsError(string password, string expectedMessage)
    {
        var result = _validator.Validate(new RegisterUserCommand("user@example.com", password));

        result.Errors.Should().Contain(error => error.PropertyName == "Password" && error.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_WithPasswordLongerThan128Characters_ReturnsError()
    {
        var result = _validator.Validate(new RegisterUserCommand("user@example.com", "Aa1!" + new string('x', 125)));

        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Password must be between 8 and 128 characters.");
    }
}
