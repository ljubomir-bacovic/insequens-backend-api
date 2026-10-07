using FluentAssertions;
using Insequens.Application.Commands.Account;
using Insequens.Application.Queries.Account;
using Insequens.Application.Validators.Account;

namespace Insequens.Application.Tests.Validators.Account;

public class AccountValidatorTests
{
    private const string Current = "Current-Passw0rd";
    private const string New = "N3w-Passw0rd!";
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void ChangePassword_WithValidCommand_ReturnsNoErrors() =>
        new ChangePasswordValidator().Validate(new ChangePasswordCommand(UserId, Current, New)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("", New, "CurrentPassword")]
    [InlineData(Current, "", "NewPassword")]
    [InlineData(Current, "weak", "NewPassword")]
    [InlineData(Current, Current, "NewPassword")]
    public void ChangePassword_WithInvalidField_ReturnsError(string current, string newPassword, string invalidField) =>
        new ChangePasswordValidator().Validate(new ChangePasswordCommand(UserId, current, newPassword))
            .Errors.Should().Contain(error => error.PropertyName == invalidField);

    [Fact]
    public void ChangePassword_WithEmptyUserId_ReturnsError() =>
        new ChangePasswordValidator().Validate(new ChangePasswordCommand(Guid.Empty, Current, New))
            .Errors.Should().ContainSingle(error => error.PropertyName == "UserId");

    [Fact]
    public void RequestEmailChange_WithValidCommand_ReturnsNoErrors() =>
        new RequestEmailChangeValidator().Validate(new RequestEmailChangeCommand(UserId, "new@example.com", Current))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData("", Current, "NewEmail")]
    [InlineData("not-an-email", Current, "NewEmail")]
    [InlineData("new@example.com", "", "CurrentPassword")]
    public void RequestEmailChange_WithInvalidField_ReturnsError(string newEmail, string current, string invalidField) =>
        new RequestEmailChangeValidator().Validate(new RequestEmailChangeCommand(UserId, newEmail, current))
            .Errors.Should().Contain(error => error.PropertyName == invalidField);

    [Fact]
    public void ConfirmEmailChange_WithValidCommand_ReturnsNoErrors() =>
        new ConfirmEmailChangeValidator().Validate(new ConfirmEmailChangeCommand(UserId.ToString(), "new@example.com", "token"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData("", "new@example.com", "token", "UserId")]
    [InlineData("id", "not-an-email", "token", "NewEmail")]
    [InlineData("id", "new@example.com", "", "Token")]
    public void ConfirmEmailChange_WithInvalidField_ReturnsError(string userId, string newEmail, string token, string invalidField) =>
        new ConfirmEmailChangeValidator().Validate(new ConfirmEmailChangeCommand(userId, newEmail, token))
            .Errors.Should().Contain(error => error.PropertyName == invalidField);

    [Fact]
    public void DeleteAccount_WithValidCommand_ReturnsNoErrors() =>
        new DeleteAccountValidator().Validate(new DeleteAccountCommand(UserId, Current)).IsValid.Should().BeTrue();

    [Fact]
    public void DeleteAccount_WithoutPassword_ReturnsError() =>
        new DeleteAccountValidator().Validate(new DeleteAccountCommand(UserId, ""))
            .Errors.Should().ContainSingle(error => error.PropertyName == "CurrentPassword");

    [Fact]
    public void DeleteAccount_WithEmptyUserId_ReturnsError() =>
        new DeleteAccountValidator().Validate(new DeleteAccountCommand(Guid.Empty, Current))
            .Errors.Should().ContainSingle(error => error.PropertyName == "UserId");

    [Fact]
    public void ExportUserData_WithUserId_ReturnsNoErrors() =>
        new ExportUserDataValidator().Validate(new ExportUserDataQuery(UserId)).IsValid.Should().BeTrue();

    [Fact]
    public void ExportUserData_WithEmptyUserId_ReturnsError() =>
        new ExportUserDataValidator().Validate(new ExportUserDataQuery(Guid.Empty))
            .Errors.Should().ContainSingle(error => error.PropertyName == "UserId");
}
