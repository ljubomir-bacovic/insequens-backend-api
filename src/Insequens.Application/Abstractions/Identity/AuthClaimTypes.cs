namespace Insequens.Application.Abstractions.Identity;

/// <summary>Claims Insequens adds to access tokens beyond the user ID and name.</summary>
public static class AuthClaimTypes
{
    /// <summary>The refresh-token family the access token was issued with, so logout can end just that session.</summary>
    public const string SessionId = "sid";
}
