using Insequens.Application.Abstractions;
using Insequens.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insequens.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string? connectionString) =>
        services.AddInsequensContext(options =>
            options.UseSqlServer(connectionString,
                providerOptions => providerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

    /// <summary>
    /// Registers the pooled <see cref="InsequensContext"/> on the given provider with the save interceptors,
    /// and exposes it as <see cref="IApplicationDbContext"/>. Tests call this to swap the provider only.
    /// </summary>
    public static IServiceCollection AddInsequensContext(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureProvider)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISaveChangesInterceptor, AuditableEntityInterceptor>());

        services.AddDbContextPool<InsequensContext>((serviceProvider, options) =>
        {
            configureProvider(options);
            options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
        });
        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<InsequensContext>());

        return services;
    }
}
