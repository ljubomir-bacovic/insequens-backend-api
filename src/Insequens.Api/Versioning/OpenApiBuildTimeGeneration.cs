using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Insequens.Api.Versioning;

/// <summary>
/// Microsoft.Extensions.ApiDescription.Server writes the OpenAPI documents on build by starting this app inside
/// its GetDocument.Insider tool, with no secrets and no database. The documents only need endpoint metadata, so
/// that run uses the Development settings (localhost only) and skips the startup validation of settings. A real
/// start is unaffected.
/// </summary>
public static class OpenApiBuildTimeGeneration
{
    public static bool IsRunning { get; } = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    public static WebApplicationOptions CreateOptions(string[] args) => new()
    {
        Args = args,
        EnvironmentName = IsRunning ? Environments.Development : null,
    };

    public static IServiceCollection SkipStartupValidationWhenGenerating(this IServiceCollection services)
    {
        if (IsRunning)
        {
            services.RemoveAll<IStartupValidator>();
        }

        return services;
    }
}
