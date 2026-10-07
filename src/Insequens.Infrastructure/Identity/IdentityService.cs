using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IIdentityService
{
    public async Task<AuthUser?> CreateUserAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
        };

        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded ? ToAuthUser(user) : null;
    }

    public async Task<AuthUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);

        return user is null || IsDeleted(user) ? null : ToAuthUser(user);
    }

    public async Task<AuthUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);

        return user is null ? null : ToAuthUser(user);
    }

    public async Task<PasswordSignInStatus> CheckPasswordSignInAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return PasswordSignInStatus.Failed;
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return PasswordSignInStatus.Succeeded;
        }

        if (result.IsLockedOut)
        {
            return PasswordSignInStatus.LockedOut;
        }

        return result.IsNotAllowed ? PasswordSignInStatus.NotAllowed : PasswordSignInStatus.Failed;
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);

        return user is not null && (await userManager.ConfirmEmailAsync(user, token)).Succeeded;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(
        Guid userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);

        return user is not null && (await userManager.ResetPasswordAsync(user, token, newPassword)).Succeeded;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);

        return user is null ? [] : [.. await userManager.GetRolesAsync(user)];
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);

        return user is not null && await userManager.IsInRoleAsync(user, role);
    }

    private static AuthUser ToAuthUser(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty);

    public async Task<AccountDetails?> GetAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);

        return new AccountDetails(user.Id, user.Email ?? string.Empty, user.EmailConfirmed, [.. roles]);
    }

    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);

        return user is not null && (await userManager.ChangePasswordAsync(user, currentPassword, newPassword)).Succeeded;
    }

    public async Task<string> GenerateChangeEmailTokenAsync(Guid userId, string newEmail, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        return await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
    }

    public async Task<bool> ChangeEmailAsync(Guid userId, string newEmail, string token, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        // Users sign in with their email, so the user name follows it; ChangeEmailAsync saves both.
        var previousUserName = user.UserName;
        user.UserName = newEmail;
        var result = await userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            user.UserName = previousUserName;
        }

        return result.Succeeded;
    }

    public async Task<bool> MarkForDeletionAsync(Guid userId, DateTime requestedAt, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.DeletionRequestedAt = requestedAt;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        // A new security stamp invalidates outstanding email-confirmation and password-reset tokens.
        await EnsureSucceededAsync(user, userManager.UpdateSecurityStampAsync(user));
        return true;
    }

    public async Task<IReadOnlyList<Guid>> FindAccountsDueForPurgeAsync(
        DateTime deletionRequestedBefore,
        CancellationToken cancellationToken) =>
        await userManager.Users
            .Where(user => user.DeletionRequestedAt != null && user.DeletionRequestedAt <= deletionRequestedBefore)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            await EnsureSucceededAsync(user, userManager.DeleteAsync(user));
        }
    }

    private static bool IsDeleted(ApplicationUser user) => user.DeletionRequestedAt is not null;

    private static async Task EnsureSucceededAsync(ApplicationUser user, Task<IdentityResult> operation)
    {
        var result = await operation;
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Updating user {user.Id} failed: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
    }

    private async Task<ApplicationUser?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null || IsDeleted(user) ? null : user;
    }

    private async Task<ApplicationUser> RequireUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await FindUserAsync(userId, cancellationToken)
        ?? throw new InvalidOperationException($"User {userId} does not exist.");
}
