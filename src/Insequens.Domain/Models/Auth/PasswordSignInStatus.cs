namespace Insequens.Domain.Models.Auth;

public enum PasswordSignInStatus
{
    Succeeded,
    Failed,
    LockedOut,
    NotAllowed,
}
