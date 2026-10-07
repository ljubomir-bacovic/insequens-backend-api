using System.Security.Claims;
using Insequens.Application.Abstractions;

namespace Insequens.Api.Security;

/// <summary>Reads the caller from the JWT on the current request. Safe as a singleton.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
}
