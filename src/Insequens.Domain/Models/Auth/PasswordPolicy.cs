namespace Insequens.Domain.Models.Auth;

/// <summary>
/// The password rules shared by ASP.NET Core Identity and the request validators, so a password
/// that passes validation is never rejected later by Identity.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;
}
