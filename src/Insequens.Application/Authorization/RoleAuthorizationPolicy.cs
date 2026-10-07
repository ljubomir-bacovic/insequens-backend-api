using System.Reflection;
using Insequens.Application.Abstractions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application.Authorization;

/// <summary>
/// For requests marked <see cref="RequiresRoleAttribute"/>, checks the caller's roles in the user store, so a role
/// removed from a user takes effect before their access token expires. Throws <see cref="ForbiddenException"/>.
/// </summary>
/// <remarks>
/// The policy runs for every request, so the user store is resolved only for requests that require a role.
/// </remarks>
public sealed class RoleAuthorizationPolicy<TRequest>(IServiceProvider serviceProvider) : IAuthorizationPolicy<TRequest>
    where TRequest : notnull
{
    private static readonly string[] RequiredRoles = typeof(TRequest)
        .GetCustomAttributes<RequiresRoleAttribute>()
        .Select(attribute => attribute.Role)
        .Distinct()
        .ToArray();

    public async Task AuthorizeAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (RequiredRoles.Length == 0)
        {
            return;
        }

        var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        if (currentUser.UserId is not { } userId)
        {
            throw new ForbiddenException(RequiredRoles[0]);
        }

        var identityService = serviceProvider.GetRequiredService<IIdentityService>();
        foreach (var role in RequiredRoles)
        {
            if (!await identityService.IsInRoleAsync(userId, role, cancellationToken))
            {
                throw new ForbiddenException(role);
            }
        }
    }
}
