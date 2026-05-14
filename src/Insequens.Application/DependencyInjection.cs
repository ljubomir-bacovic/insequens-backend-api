using AutoMapper;
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
        services.AddValidatorsFromAssembly(assembly);
        services.AddSingleton(_ => new MapperConfiguration(configuration => configuration.AddMaps(assembly)));
        services.AddSingleton<IMapper>(serviceProvider =>
            serviceProvider.GetRequiredService<MapperConfiguration>().CreateMapper(type => serviceProvider.GetRequiredService(type)));

        return services;
    }
}
