namespace Insequens.Api;

public static class RequestLimits
{
    /// <summary>1 MB. Raise it for the endpoints that accept attachments once they exist.</summary>
    public const long MaxRequestBodySize = 1024 * 1024;
}
