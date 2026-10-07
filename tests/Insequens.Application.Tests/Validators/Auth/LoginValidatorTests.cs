using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Validators.Auth;
using Insequens.Domain.Entities;

namespace Insequens.Application.Tests.Validators.Auth;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        // Login checks only presence, not the password policy, so older passwords still work.
        _validator.Validate(new LoginCommand("user@example.com", "x")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "x", "Email")]
    [InlineData("not-an-email", "x", "Email")]
    [InlineData("user@example.com", "", "Password")]
    public void Validate_WithInvalidField_ReturnsError(string email, string password, string invalidField)
    {
        var result = _validator.Validate(new LoginCommand(email, password));

        result.Errors.Should().Contain(error => error.PropertyName == invalidField);
    }

    [Fact]
    public void Validate_WithDeviceNameAtMaximumLength_ReturnsNoErrors()
    {
        var command = new LoginCommand("user@example.com", "x", new string('d', RefreshToken.DeviceNameMaxLength));

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDeviceNameOverMaximumLength_ReturnsError()
    {
        var command = new LoginCommand("user@example.com", "x", new string('d', RefreshToken.DeviceNameMaxLength + 1));

        _validator.Validate(command).Errors.Should().ContainSingle(error => error.PropertyName == "DeviceName");
    }
}
