using Insequens.Domain.Models.Auth;

namespace Insequens.Domain.ServiceContracts;

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

    Task StoreRefreshTokenAsync(Guid userId, IssuedRefreshToken refreshToken, CancellationToken cancellationToken);

    Task<bool> ValidateRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken);

    Task RevokeRefreshTokenAsync(Guid userId, CancellationToken cancellationToken);
}
