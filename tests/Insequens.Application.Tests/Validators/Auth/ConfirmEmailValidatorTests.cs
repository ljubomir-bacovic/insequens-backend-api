using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Tests.Validators.Auth;

public class ConfirmEmailValidatorTests
{
    private readonly ConfirmEmailValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        _validator.Validate(new ConfirmEmailCommand(Guid.NewGuid().ToString(), "token")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "token", "UserId")]
    [InlineData("user-id", "", "Token")]
    public void Validate_WithEmptyField_ReturnsError(string userId, string token, string invalidField)
    {
        _validator.Validate(new ConfirmEmailCommand(userId, token)).Errors.Should().ContainSingle(error => error.PropertyName == invalidField);
    }
}
