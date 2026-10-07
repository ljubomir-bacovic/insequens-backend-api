namespace Insequens.Application.Abstractions.Identity;

public enum PasswordSignInStatus
{
    Succeeded,
    Failed,
    LockedOut,
    NotAllowed,
}
