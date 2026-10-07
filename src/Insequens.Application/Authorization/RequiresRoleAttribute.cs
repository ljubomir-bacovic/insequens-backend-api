namespace Insequens.Application.Authorization;

/// <summary>
/// Restricts a request to callers in <see cref="Role"/>, checked by <see cref="RoleAuthorizationPolicy{TRequest}"/>
/// against the user store. Several attributes all apply.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequiresRoleAttribute(string role) : Attribute
{
    public string Role { get; } = role;
}
