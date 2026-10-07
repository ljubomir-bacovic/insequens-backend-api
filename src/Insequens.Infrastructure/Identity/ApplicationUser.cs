using Microsoft.AspNetCore.Identity;

namespace Insequens.Infrastructure.Identity;

/// <summary>An account. Keyed by <see cref="Guid"/>, the same type as <c>UserId</c> on owned entities.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Set when the owner deletes the account. The account cannot sign in from then on, and is purged with all its
    /// data once the grace period has passed.
    /// </summary>
    public DateTime? DeletionRequestedAt { get; set; }
}
