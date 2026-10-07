using System.Security.Cryptography;
using System.Text;

namespace Insequens.Application.Commands.Auth;

/// <summary>Only a SHA-256 hash of a refresh token is stored, so a copy of the database yields no usable tokens.</summary>
internal static class RefreshTokenHash
{
    public static string Compute(string refreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
