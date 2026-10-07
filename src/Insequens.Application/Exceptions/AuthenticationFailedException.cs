namespace Insequens.Application.Exceptions;

/// <summary>
/// Thrown for every failed login or token refresh, whatever the reason, so the response never reveals
/// whether an account exists, is unconfirmed or is locked out.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Authentication failed.")
    {
    }
}
