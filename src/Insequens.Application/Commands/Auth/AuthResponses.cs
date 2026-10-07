using Insequens.Domain.Models.Auth;

namespace Insequens.Application.Commands.Auth;

/// <summary>
/// Fixed response bodies. Register, forgot-password and reset-password return the same body whether
/// or not the email belongs to an account, so the responses cannot be used to enumerate accounts.
/// </summary>
public static class AuthResponses
{
    public static AuthMessageResponse RegistrationAccepted { get; } =
        new("If this email address can be registered, a confirmation email has been sent to it.");

    public static AuthMessageResponse PasswordResetRequested { get; } =
        new("If an account exists for this email address, a password reset email has been sent to it.");

    public static AuthMessageResponse PasswordResetAccepted { get; } =
        new("If the reset token was valid, the password has been changed.");

    public static AuthMessageResponse EmailConfirmed { get; } = new("Email confirmed successfully!");

    public static AuthMessageResponse LoggedOut { get; } = new("User logged out successfully.");
}
