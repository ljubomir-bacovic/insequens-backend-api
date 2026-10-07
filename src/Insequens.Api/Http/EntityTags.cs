using Microsoft.Net.Http.Headers;

namespace Insequens.Api.Http;

/// <summary>
/// Strong ETags for optimistic concurrency: the entity's row version in Base64. A client sends the ETag it last
/// read as <c>If-Match</c>, and a change made since then fails with 412.
/// </summary>
public static class EntityTags
{
    public static string Format(byte[] version) => $"\"{Convert.ToBase64String(version)}\"";

    /// <summary>
    /// Reads <c>If-Match</c>. No header or <c>*</c> gives a null version (no check). One strong ETag of ours gives
    /// its version. Anything else (malformed, weak, or several tags) cannot match and returns false.
    /// </summary>
    public static bool TryParseIfMatch(HttpRequest request, out byte[]? expectedVersion)
    {
        expectedVersion = null;

        if (request.Headers.IfMatch.Count == 0)
        {
            return true;
        }

        if (!EntityTagHeaderValue.TryParseList(request.Headers.IfMatch, out var tags) || tags.Count == 0)
        {
            return false;
        }

        if (tags.Any(tag => tag.Equals(EntityTagHeaderValue.Any)))
        {
            return true;
        }

        if (tags.Count != 1 || tags[0].IsWeak)
        {
            return false;
        }

        var base64 = tags[0].Tag.AsSpan()[1..^1];
        var buffer = new byte[base64.Length];
        if (!Convert.TryFromBase64Chars(base64, buffer, out var length))
        {
            return false;
        }

        expectedVersion = buffer[..length];
        return true;
    }
}
