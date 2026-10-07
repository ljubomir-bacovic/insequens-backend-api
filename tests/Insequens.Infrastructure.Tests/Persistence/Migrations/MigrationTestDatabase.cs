using Insequens.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

/// <summary>Raw SQL against a migration test database, so data can be seeded in a schema older than the model.</summary>
internal static class MigrationTestDatabase
{
    public static Task MigrateAsync(this InsequensContext context, string targetMigration) =>
        context.GetService<IMigrator>().MigrateAsync(targetMigration);

    public static async Task ExecuteAsync(this InsequensContext context, string sql)
    {
        await using var connection = await OpenAsync(context);
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<T> ScalarAsync<T>(this InsequensContext context, string sql)
    {
        await using var connection = await OpenAsync(context);
        await using var command = new SqlCommand(sql, connection);

        return (T)(await command.ExecuteScalarAsync())!;
    }

    public static Task<string> ColumnTypeAsync(this InsequensContext context, string table, string column) =>
        context.ScalarAsync<string>(
            $"SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'");

    private static async Task<SqlConnection> OpenAsync(InsequensContext context)
    {
        var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();

        return connection;
    }
}
