namespace Insequens.Application.Abstractions.Identity;

/// <summary>
/// The user store. Accounts scheduled for deletion are treated as missing by every method except
/// <see cref="FindAccountsDueForPurgeAsync"/> and <see cref="DeleteUserAsync"/>.
/// </summary>
public interface IIdentityService
{
    Task<AuthUser?> CreateUserAsync(string email, string password, CancellationToken cancellationToken);

    Task<AuthUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<AuthUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<PasswordSignInStatus> CheckPasswordSignInAsync(Guid userId, string password, CancellationToken cancellationToken);

    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken);

    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken);

    Task<AccountDetails?> GetAccountAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<string> GenerateChangeEmailTokenAsync(Guid userId, string newEmail, CancellationToken cancellationToken);

    /// <summary>Changes the email and the sign-in name together, if the token was issued for this new email.</summary>
    Task<bool> ChangeEmailAsync(Guid userId, string newEmail, string token, CancellationToken cancellationToken);

    /// <summary>Disables sign-in and records when deletion was requested. False when the account does not exist.</summary>
    Task<bool> MarkForDeletionAsync(Guid userId, DateTime requestedAt, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> FindAccountsDueForPurgeAsync(DateTime deletionRequestedBefore, CancellationToken cancellationToken);

    /// <summary>Permanently deletes the account with its roles, claims, logins and tokens.</summary>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);
}
