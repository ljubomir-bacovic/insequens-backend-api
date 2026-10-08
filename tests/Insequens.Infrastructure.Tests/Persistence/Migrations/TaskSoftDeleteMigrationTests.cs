using FluentAssertions;
using Insequens.Infrastructure.Persistence;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

[Collection(SqlServerCollection.Name)]
public sealed class TaskSoftDeleteMigrationTests(SqlServerFixture sqlServer)
{
    private const string PreviousMigration = "20261007144229_TaskPriorityNone";
    private const string Migration = "20261008075722_TaskSoftDelete";

    private static readonly Guid UserId = Guid.Parse("3c9a1d2e-5f6b-4c7d-8e9f-2a3b4c5d6e7f");
    private static readonly Guid TaskId = Guid.Parse("8d2f3e4a-5b6c-4d7e-9f0a-1b2c3d4e5f6a");

    [Fact]
    public async Task Migrate_KeepsEveryExistingTaskLive()
    {
        await using var context = await CreateWithTaskAsync();

        await context.MigrateAsync(Migration);

        (await context.ScalarAsync<bool>($"SELECT IsDeleted FROM Tasks WHERE Id = '{TaskId}'")).Should().BeFalse();
        (await context.ScalarAsync<int>($"SELECT COUNT(*) FROM Tasks WHERE Id = '{TaskId}' AND DeletedOn IS NULL AND DeletedBy IS NULL"))
            .Should().Be(1);
    }

    [Fact]
    public async Task Migrate_IndexesTheTrashInTheListIndexesAndThePurge()
    {
        await using var context = await CreateWithTaskAsync();

        await context.MigrateAsync(Migration);

        (await IndexColumnsAsync(context, "IX_Tasks_UserId_IsDeleted_IsCompleted_DueDate")).Should().Be("UserId,IsDeleted,IsCompleted,DueDate");
        (await IndexColumnsAsync(context, "IX_Tasks_UserId_IsDeleted_CreatedOn")).Should().Be("UserId,IsDeleted,CreatedOn");
        (await context.ScalarAsync<string>("SELECT filter_definition FROM sys.indexes WHERE name = 'IX_Tasks_DeletedOn'"))
            .Should().Be("([IsDeleted]=(1))");
    }

    [Fact]
    public async Task Down_RemovesTheColumnsAndKeepsTheTask()
    {
        await using var context = await CreateWithTaskAsync();
        await context.MigrateAsync(Migration);

        await context.MigrateAsync(PreviousMigration);

        (await context.ScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tasks' AND COLUMN_NAME IN ('IsDeleted', 'DeletedOn', 'DeletedBy')"))
            .Should().Be(0);
        (await context.ScalarAsync<string>($"SELECT Name FROM Tasks WHERE Id = '{TaskId}'")).Should().Be("Task");
    }

    private async Task<InsequensContext> CreateWithTaskAsync()
    {
        var context = sqlServer.CreateContext();
        await context.MigrateAsync(PreviousMigration);
        await context.ExecuteAsync($"""
            INSERT INTO AspNetUsers (Id, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES ('{UserId}', 1, 0, 0, 1, 0);
            INSERT INTO Tasks (Id, UserId, Name, Priority, IsCompleted, CreatedOn, UpdatedOn)
            VALUES ('{TaskId}', '{UserId}', 'Task', 0, 0, '2026-01-01', '2026-01-01');
            """);

        return context;
    }

    private static Task<string> IndexColumnsAsync(InsequensContext context, string index) =>
        context.ScalarAsync<string>($"""
            SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.name = '{index}'
            """);
}
