using FluentAssertions;
using Insequens.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

[Collection(SqlServerCollection.Name)]
public sealed class TaskSchemaHardeningMigrationTests(SqlServerFixture sqlServer)
{
    private const string PreviousMigration = "20261007123539_AccountDeletion";
    private const string Migration = "20261007133729_TaskSchemaHardening";

    private static readonly Guid UserId = Guid.Parse("2b8f0c1d-4e5a-4b6c-9d7e-1f2a3b4c5d6e");
    private static readonly Guid TaskId = Guid.Parse("7c1e2d3f-4a5b-4c6d-8e9f-0a1b2c3d4e5f");

    [Fact]
    public async Task Migrate_RenamesTheTableAndKeepsEveryTask()
    {
        await using var context = await CreateWithTaskAsync();

        await context.MigrateAsync(Migration);

        (await TableCountAsync(context, "ToDoItem")).Should().Be(0);
        (await context.ScalarAsync<string>($"SELECT Name FROM Tasks WHERE Id = '{TaskId}'")).Should().Be("Task");
        (await context.ScalarAsync<int>($"SELECT Priority FROM Tasks WHERE Id = '{TaskId}'")).Should().Be(1);
        (await context.ColumnTypeAsync("Tasks", "RowVersion")).Should().Be("timestamp");
        (await MaxLengthAsync(context, "Name")).Should().Be(200);
        (await MaxLengthAsync(context, "Description")).Should().Be(4000);
    }

    [Fact]
    public async Task Migrate_TheDatabaseRejectsALongNameAndAnUnknownPriority()
    {
        await using var context = await CreateWithTaskAsync();
        await context.MigrateAsync(Migration);

        var longName = () => context.ExecuteAsync(InsertTask(Guid.NewGuid(), name: new string('a', 201)));
        var unknownPriority = () => context.ExecuteAsync(InsertTask(Guid.NewGuid(), priority: "4"));
        var noPriority = () => context.ExecuteAsync(InsertTask(Guid.NewGuid(), priority: "NULL"));

        await longName.Should().ThrowAsync<SqlException>().WithMessage("*truncated*");
        await unknownPriority.Should().ThrowAsync<SqlException>().WithMessage("*CK_Tasks_Priority*");
        await noPriority.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Migrate_TheRowVersionChangesOnEveryUpdate()
    {
        await using var context = await CreateWithTaskAsync();
        await context.MigrateAsync(Migration);
        var before = await RowVersionAsync(context);

        await context.ExecuteAsync($"UPDATE Tasks SET IsCompleted = 1 WHERE Id = '{TaskId}'");

        (await RowVersionAsync(context)).Should().NotEqual(before);
    }

    [Theory]
    [InlineData("Name = REPLICATE('a', 201)", "50004")]
    [InlineData("Description = REPLICATE(CAST('a' AS nvarchar(max)), 4001)", "50005")]
    [InlineData("Priority = 7", "50006")]
    public async Task Migrate_WithATaskThatDoesNotFit_FailsWithAClearMessageAndChangesNothing(string update, string error)
    {
        await using var context = await CreateWithTaskAsync();
        await context.ExecuteAsync($"UPDATE ToDoItem SET {update} WHERE Id = '{TaskId}'");

        var migrate = () => context.MigrateAsync(Migration);

        (await migrate.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(int.Parse(error));
        (await TableCountAsync(context, "ToDoItem")).Should().Be(1);
        (await TableCountAsync(context, "Tasks")).Should().Be(0);
    }

    [Fact]
    public async Task Down_RestoresTheToDoItemTableWithItsRows()
    {
        await using var context = await CreateWithTaskAsync();
        await context.MigrateAsync(Migration);

        await context.MigrateAsync(PreviousMigration);

        (await TableCountAsync(context, "Tasks")).Should().Be(0);
        (await context.ScalarAsync<string>($"SELECT Name FROM ToDoItem WHERE Id = '{TaskId}'")).Should().Be("Task");
    }

    private async Task<InsequensContext> CreateWithTaskAsync()
    {
        var context = sqlServer.CreateContext();
        await context.MigrateAsync(PreviousMigration);
        await context.ExecuteAsync($"""
            INSERT INTO AspNetUsers (Id, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES ('{UserId}', 1, 0, 0, 1, 0);
            """);
        await context.ExecuteAsync(InsertTask(TaskId, table: "ToDoItem"));

        return context;
    }

    private static string InsertTask(Guid taskId, string name = "Task", string priority = "1", string table = "Tasks") => $"""
        INSERT INTO {table} (Id, UserId, Name, Priority, IsCompleted, CreatedOn, UpdatedOn)
        VALUES ('{taskId}', '{UserId}', '{name}', {priority}, 0, '2026-01-01', '2026-01-01');
        """;

    private static Task<int> TableCountAsync(InsequensContext context, string table) =>
        context.ScalarAsync<int>($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}'");

    private static Task<int> MaxLengthAsync(InsequensContext context, string column) =>
        context.ScalarAsync<int>(
            $"SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tasks' AND COLUMN_NAME = '{column}'");

    private static Task<byte[]> RowVersionAsync(InsequensContext context) =>
        context.ScalarAsync<byte[]>($"SELECT RowVersion FROM Tasks WHERE Id = '{TaskId}'");
}
