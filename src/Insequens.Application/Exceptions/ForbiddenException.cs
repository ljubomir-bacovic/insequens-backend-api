namespace Insequens.Application.Exceptions;

/// <summary>
/// The caller is authenticated but lacks a role the request requires (403). A resource the caller does not own is
/// a <see cref="NotFoundException"/> instead, so IDs cannot be probed.
/// </summary>
public sealed class ForbiddenException(string requiredRole) : Exception($"The {requiredRole} role is required.")
{
    public string RequiredRole { get; } = requiredRole;
}
