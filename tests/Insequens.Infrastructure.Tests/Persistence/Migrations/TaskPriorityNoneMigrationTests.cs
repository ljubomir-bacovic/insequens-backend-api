using FluentAssertions;
using Insequens.Infrastructure.Persistence;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

[Collection(SqlServerCollection.Name)]
public sealed class TaskPriorityNoneMigrationTests(SqlServerFixture sqlServer)
{
    private const string PreviousMigration = "20261007133729_TaskSchemaHardening";
    private const string Migration = "20261007144229_TaskPriorityNone";

    private static readonly Guid UserId = Guid.Parse("5e2d1c0b-9a8f-4e7d-8c6b-5a4f3e2d1c0b");

    /// <summary>Old value (v1: 1 high, 2 medium, 3 low, 0 or NULL none) and the value after the migration.</summary>
    private static readonly (string Name, string Old, int New)[] Tasks =
    [
        ("none-null", "NULL", 0),
        ("none-zero", "0", 0),
        ("high", "1", 3),
        ("medium", "2", 2),
        ("low", "3", 1),
    ];

    [Fact]
    public async Task Migrate_ReordersPrioritiesToAscendingImportanceAndRequiresOne()
    {
        await using var context = await CreateWithTasksAsync();

        await context.MigrateAsync(Migration);

        foreach (var (name, _, expected) in Tasks)
        {
            (await PriorityAsync(context, name)).Should().Be(expected, name);
        }

        (await context.ScalarAsync<string>(
                "SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tasks' AND COLUMN_NAME = 'Priority'"))
            .Should().Be("NO");
        (await context.ScalarAsync<int>("SELECT COUNT(*) FROM sys.check_constraints WHERE name = 'CK_Tasks_Priority'"))
            .Should().Be(1);
    }

    [Fact]
    public async Task Down_RestoresTheV1ValuesWithNullForNone()
    {
        await using var context = await CreateWithTasksAsync();
        await context.MigrateAsync(Migration);

        await context.MigrateAsync(PreviousMigration);

        (await context.ScalarAsync<int>("SELECT COUNT(*) FROM Tasks WHERE Priority IS NULL")).Should().Be(2);
        (await PriorityAsync(context, "high")).Should().Be(1);
        (await PriorityAsync(context, "medium")).Should().Be(2);
        (await PriorityAsync(context, "low")).Should().Be(3);
    }

    private async Task<InsequensContext> CreateWithTasksAsync()
    {
        var context = sqlServer.CreateContext();
        await context.MigrateAsync(PreviousMigration);
        await context.ExecuteAsync($"""
            INSERT INTO AspNetUsers (Id, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES ('{UserId}', 1, 0, 0, 1, 0);
            """);

        foreach (var (name, old, _) in Tasks)
        {
            await context.ExecuteAsync($"""
                INSERT INTO Tasks (Id, UserId, Name, Priority, IsCompleted, CreatedOn, UpdatedOn)
                VALUES ('{Guid.NewGuid()}', '{UserId}', '{name}', {old}, 0, '2026-01-01', '2026-01-01');
                """);
        }

        return context;
    }

    private static Task<int> PriorityAsync(InsequensContext context, string name) =>
        context.ScalarAsync<int>($"SELECT Priority FROM Tasks WHERE Name = '{name}'");
}
