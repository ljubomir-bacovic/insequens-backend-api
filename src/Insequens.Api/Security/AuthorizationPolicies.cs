namespace Insequens.Api.Security;

/// <summary>Names for <c>[Authorize(Policy = ...)]</c>.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Authenticated, with the Admin role in the access token. The Application layer checks the role again.</summary>
    public const string Admin = "Admin";
}
