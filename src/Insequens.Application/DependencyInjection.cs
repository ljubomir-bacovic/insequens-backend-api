using FluentValidation;
using Insequens.Application.Authorization;
using Insequens.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddAutoMapper(configuration => { }, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        // Policies run in registration order: a missing role is a 403 before any resource is loaded.
        services.AddTransient(typeof(IAuthorizationPolicy<>), typeof(RoleAuthorizationPolicy<>));
        services.AddTransient(typeof(IAuthorizationPolicy<>), typeof(OwnershipAuthorizationPolicy<>));
        services.AddScoped(typeof(IOwnershipPolicy<>), typeof(OwnershipPolicy<>));
        services.AddScoped(typeof(IResourceContext<>), typeof(ResourceContext<>));

        return services;
    }
}
