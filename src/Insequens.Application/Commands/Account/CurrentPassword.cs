using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Exceptions;

namespace Insequens.Application.Commands.Account;

internal static class CurrentPassword
{
    /// <summary>
    /// Confirms the signed-in user knows the password before a sensitive change. Failures count towards lockout,
    /// so the account endpoints cannot be used to guess it.
    /// </summary>
    public static async Task VerifyAsync(
        IIdentityService identityService,
        Guid userId,
        string password,
        CancellationToken cancellationToken)
    {
        var status = await identityService.CheckPasswordSignInAsync(userId, password, cancellationToken);
        switch (status)
        {
            case PasswordSignInStatus.Succeeded:
                return;
            case PasswordSignInStatus.LockedOut:
                throw new AccountUpdateFailedException("Too many failed attempts. Try again later.");
            default:
                throw new AccountUpdateFailedException("The current password is incorrect.");
        }
    }
}
