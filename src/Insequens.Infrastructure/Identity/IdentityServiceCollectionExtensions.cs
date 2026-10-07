using Insequens.Infrastructure.Persistence;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Insequens.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = PasswordPolicy.MinimumLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<InsequensContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtActiveKeyValidator>();

        services.AddSingleton<IJwtKeyRing, JwtKeyRing>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}
