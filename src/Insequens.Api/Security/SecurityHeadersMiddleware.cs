namespace Insequens.Api.Security;

/// <summary>Adds browser security headers to every response, including error responses.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string PermissionsPolicy =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = PermissionsPolicy;

            return Task.CompletedTask;
        });

        return next(context);
    }
}
