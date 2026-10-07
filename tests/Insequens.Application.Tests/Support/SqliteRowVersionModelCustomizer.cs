using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Insequens.Application.Tests.Support;

/// <summary>
/// SQLite has no rowversion, so a SQL Server rowversion column would be NOT NULL with no value. This gives each
/// one a random default, enough for tests that do not test concurrency; those run on SQL Server.
/// </summary>
public sealed class SqliteRowVersionModelCustomizer(ModelCustomizerDependencies dependencies)
    : RelationalModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        var rowVersions = modelBuilder.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(byte[])
                && property.IsConcurrencyToken
                && property.ValueGenerated == ValueGenerated.OnAddOrUpdate);

        foreach (var rowVersion in rowVersions)
        {
            rowVersion.SetDefaultValueSql("randomblob(8)");
        }
    }
}
