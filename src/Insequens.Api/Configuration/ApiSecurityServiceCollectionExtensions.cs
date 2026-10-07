using System.Net;
using Insequens.Api.Security;
using Insequens.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Insequens.Api.Configuration;

public static class ApiSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();

        // Every endpoint requires an authenticated user unless it opts out with [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CorsOptions>, CorsOptionsValidator>();
        services.AddCors();
        services.ConfigureOptions<ConfigureCorsPolicy>();

        services.AddOptions<HostFilteringOptions>().ValidateOnStart();
        services.AddSingleton<IValidateOptions<HostFilteringOptions>, AllowedHostsValidator>();

        services.AddOptions<ReverseProxyOptions>()
            .Bind(configuration.GetSection(ReverseProxyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ReverseProxyOptions>>((forwardedHeaders, reverseProxy) =>
            {
                forwardedHeaders.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                // Trust only the configured proxies, not the loopback defaults.
                forwardedHeaders.KnownProxies.Clear();
                forwardedHeaders.KnownIPNetworks.Clear();

                foreach (var proxy in reverseProxy.Value.KnownProxies)
                {
                    forwardedHeaders.KnownProxies.Add(IPAddress.Parse(proxy));
                }

                foreach (var network in reverseProxy.Value.KnownNetworks)
                {
                    forwardedHeaders.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                }
            });

        return services;
    }
}
