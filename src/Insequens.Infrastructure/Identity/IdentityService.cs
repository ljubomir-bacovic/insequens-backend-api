using Microsoft.AspNetCore.Identity;
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

        return user is null ? null : ToAuthUser(user);
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

    private async Task<ApplicationUser?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await userManager.FindByIdAsync(userId.ToString());
    }

    private async Task<ApplicationUser> RequireUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await FindUserAsync(userId, cancellationToken)
        ?? throw new InvalidOperationException($"User {userId} does not exist.");
}
