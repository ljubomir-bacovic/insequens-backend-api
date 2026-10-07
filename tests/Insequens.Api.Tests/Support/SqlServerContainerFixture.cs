using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Insequens.Api.Tests.Support;

/// <summary>
/// A SQL Server container (Docker required) for API tests that need real rowversion and locking. Each call to
/// <see cref="NewDatabase"/> names a new database, which the factory creates. INS-070 merges this with the
/// Infrastructure tests' fixture.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string NewDatabase() => new SqlConnectionStringBuilder(_container.GetConnectionString())
    {
        InitialCatalog = $"insequens_api_{Guid.NewGuid():N}",
    }.ConnectionString;
}
