using Insequens.Domain.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insequens.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string? connectionString)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContextPool<InsequensContext>(options =>
            options.UseSqlServer(connectionString,
                providerOptions => providerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

        services.AddScoped<IDataContext, DataContext>();

        return services;
    }
}
