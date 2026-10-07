using FluentAssertions;
using Insequens.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

[Collection(SqlServerCollection.Name)]
public sealed class IdentityGuidKeysMigrationTests(SqlServerFixture sqlServer)
{
    private const string LastV1Migration = "20261007104648_ToDoItemAuditActors";
    private const string Migration = "20261007115847_IdentityGuidKeys";

    private static readonly Guid UserId = Guid.Parse("6f1c2a0e-9b7d-4c1e-8a35-2d4f6b8e1c90");
    private static readonly Guid RoleId = Guid.Parse("0d5b8e2a-3c4f-4a6b-9e1d-7f2c8a9b0e31");
    private static readonly Guid TaskId = Guid.Parse("a3e9c1d4-5f6b-4e7a-8c9d-0b1e2f3a4c5d");

    private static readonly (string Table, string Column)[] KeyColumns =
    [
        ("AspNetUsers", "Id"),
        ("AspNetRoles", "Id"),
        ("AspNetUserRoles", "UserId"),
        ("AspNetUserRoles", "RoleId"),
        ("AspNetUserClaims", "UserId"),
        ("AspNetUserLogins", "UserId"),
        ("AspNetUserTokens", "UserId"),
        ("AspNetRoleClaims", "RoleId"),
    ];

    [Fact]
    public async Task Migrate_FromV1DataWithStringKeys_ConvertsKeysAndKeepsEveryRow()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(LastV1Migration);
        await SeedV1DataAsync(context, UserId.ToString());

        await context.MigrateAsync(Migration);

        (await KeyColumnTypesAsync(context)).Should().AllBe("uniqueidentifier");
        (await context.ScalarAsync<Guid>("SELECT Id FROM AspNetUsers")).Should().Be(UserId);
        (await CountAsync(context, $"AspNetUserRoles WHERE UserId = '{UserId}' AND RoleId = '{RoleId}'")).Should().Be(1);
        (await CountAsync(context, $"AspNetUserClaims WHERE UserId = '{UserId}'")).Should().Be(1);
        (await CountAsync(context, $"AspNetUserLogins WHERE UserId = '{UserId}'")).Should().Be(1);
        (await CountAsync(context, $"AspNetUserTokens WHERE UserId = '{UserId}'")).Should().Be(1);
        (await CountAsync(context, $"AspNetRoleClaims WHERE RoleId = '{RoleId}'")).Should().Be(1);
        (await context.ScalarAsync<Guid>($"SELECT UserId FROM ToDoItem WHERE Id = '{TaskId}'")).Should().Be(UserId);
    }

    [Fact]
    public async Task Migrate_FromV1Data_EnforcesTaskOwnersAndDeletesTasksWithTheirUser()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(LastV1Migration);
        await SeedV1DataAsync(context, UserId.ToString());
        await context.MigrateAsync(Migration);

        var insertOrphan = () => context.ExecuteAsync(InsertTask(Guid.NewGuid(), Guid.NewGuid().ToString()));
        await insertOrphan.Should().ThrowAsync<SqlException>().WithMessage("*FK_ToDoItem_AspNetUsers_UserId*");

        await context.ExecuteAsync($"DELETE FROM AspNetUserRoles; DELETE FROM AspNetUsers WHERE Id = '{UserId}'");

        (await CountAsync(context, "ToDoItem")).Should().Be(0);
    }

    [Fact]
    public async Task Migrate_WithUserIdThatIsNotAGuid_FailsAndLeavesTheSchemaUnchanged()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(LastV1Migration);
        await SeedV1DataAsync(context, "not-a-guid", taskOwner: UserId.ToString());

        var migrate = () => context.MigrateAsync(Migration);

        await migrate.Should().ThrowAsync<SqlException>().WithMessage("*not a GUID*");
        (await KeyColumnTypesAsync(context)).Should().AllBe("nvarchar");
        (await context.Database.GetAppliedMigrationsAsync()).Should().NotContain(Migration);
    }

    [Fact]
    public async Task Migrate_WithTaskWhoseOwnerDoesNotExist_FailsAndLeavesTheSchemaUnchanged()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(LastV1Migration);
        await SeedV1DataAsync(context, UserId.ToString(), taskOwner: Guid.NewGuid().ToString());

        var migrate = () => context.MigrateAsync(Migration);

        await migrate.Should().ThrowAsync<SqlException>().WithMessage("*owner does not exist*");
        (await KeyColumnTypesAsync(context)).Should().AllBe("nvarchar");
        (await context.Database.GetAppliedMigrationsAsync()).Should().NotContain(Migration);
    }

    [Fact]
    public async Task Migrate_BackToV1_RestoresStringKeysAndKeepsEveryRow()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(LastV1Migration);
        await SeedV1DataAsync(context, UserId.ToString());
        await context.MigrateAsync(Migration);

        await context.MigrateAsync(LastV1Migration);

        (await KeyColumnTypesAsync(context)).Should().AllBe("nvarchar");
        (await context.ScalarAsync<string>("SELECT Id FROM AspNetUsers")).Should().BeEquivalentTo(UserId.ToString());
        (await CountAsync(context, "AspNetUserRoles")).Should().Be(1);
        (await CountAsync(context, "ToDoItem")).Should().Be(1);
    }

    [Fact]
    public async Task Migrate_FromScratch_AppliesEveryMigrationAndMatchesTheModel()
    {
        await using var context = sqlServer.CreateContext();

        await context.Database.MigrateAsync();

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        context.Database.HasPendingModelChanges().Should().BeFalse();
        (await CountAsync(context, "AspNetRoles WHERE NormalizedName IN ('ADMIN', 'SUPPORT')")).Should().Be(2);
    }

    private static async Task SeedV1DataAsync(InsequensContext context, string userId, string? taskOwner = null)
    {
        await context.ExecuteAsync($"""
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, RefreshTokenExpiryTime)
            VALUES ('{userId}', 'user@example.com', 'USER@EXAMPLE.COM', 'user@example.com', 'USER@EXAMPLE.COM', 1,
                0, 0, 1, 0, '2026-01-01');
            INSERT INTO AspNetRoles (Id, Name, NormalizedName) VALUES ('{RoleId}', 'Tester', 'TESTER');
            INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ('{userId}', '{RoleId}');
            INSERT INTO AspNetUserClaims (UserId, ClaimType, ClaimValue) VALUES ('{userId}', 'locale', 'en');
            INSERT INTO AspNetUserLogins (LoginProvider, ProviderKey, UserId) VALUES ('Example', 'key-1', '{userId}');
            INSERT INTO AspNetUserTokens (UserId, LoginProvider, Name, Value) VALUES ('{userId}', 'Example', 'token', 'value');
            INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue) VALUES ('{RoleId}', 'permission', 'read');
            """);
        await context.ExecuteAsync(InsertTask(TaskId, taskOwner ?? userId));
    }

    private static string InsertTask(Guid taskId, string ownerId) => $"""
        INSERT INTO ToDoItem (Id, UserId, Name, IsCompleted, CreatedOn, UpdatedOn)
        VALUES ('{taskId}', '{ownerId}', 'Task', 0, '2026-01-01', '2026-01-01');
        """;

    private static Task<int> CountAsync(InsequensContext context, string tableAndFilter) =>
        context.ScalarAsync<int>($"SELECT COUNT(*) FROM {tableAndFilter}");

    private static async Task<string[]> KeyColumnTypesAsync(InsequensContext context)
    {
        var types = new List<string>();
        foreach (var (table, column) in KeyColumns)
        {
            types.Add(await context.ColumnTypeAsync(table, column));
        }

        return [.. types];
    }
}
