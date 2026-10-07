using Insequens.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

/// <summary>
/// One SQL Server container per test run (Docker required). Each test gets its own empty database on it.
/// INS-070 grows this into the shared test infrastructure.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>A context on a new, empty database. The database is created by the first migration.</summary>
    public InsequensContext CreateContext()
    {
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = $"insequens_{Guid.NewGuid():N}",
        }.ConnectionString;

        return new InsequensContext(new DbContextOptionsBuilder<InsequensContext>()
            .UseSqlServer(connectionString)
            .Options);
    }
}
