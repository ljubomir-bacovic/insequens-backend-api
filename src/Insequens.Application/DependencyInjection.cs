using FluentValidation;
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

        return services;
    }
}
