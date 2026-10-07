using Microsoft.AspNetCore.Identity;

namespace Insequens.Infrastructure.Identity;

/// <summary>An account. Keyed by <see cref="Guid"/>, the same type as <c>UserId</c> on owned entities.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}
