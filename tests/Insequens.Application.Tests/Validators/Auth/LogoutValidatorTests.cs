using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Tests.Validators.Auth;

public class LogoutValidatorTests
{
    private readonly LogoutValidator _validator = new();

    [Fact]
    public void Validate_WithUserId_ReturnsNoErrors()
    {
        _validator.Validate(new LogoutCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsError()
    {
        _validator.Validate(new LogoutCommand(Guid.Empty)).Errors.Should().ContainSingle(error => error.PropertyName == "UserId");
    }
}
