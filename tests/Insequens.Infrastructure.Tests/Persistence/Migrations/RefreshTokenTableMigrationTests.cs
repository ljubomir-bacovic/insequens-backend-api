using FluentAssertions;
using Insequens.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

[Collection(SqlServerCollection.Name)]
public sealed class RefreshTokenTableMigrationTests(SqlServerFixture sqlServer)
{
    private const string PreviousMigration = "20261007115847_IdentityGuidKeys";
    private const string Migration = "20261007121129_RefreshTokenTable";

    [Fact]
    public async Task Migrate_DropsTheTokenColumnsFromUsersAndKeepsTheUsers()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(PreviousMigration);
        var userId = Guid.NewGuid();
        await context.ExecuteAsync($"""
            INSERT INTO AspNetUsers (Id, UserName, Email, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
                LockoutEnabled, AccessFailedCount, RefreshToken, RefreshTokenExpiryTime)
            VALUES ('{userId}', 'user@example.com', 'user@example.com', 1, 0, 0, 1, 0, 'old-token-hash', '2026-01-01');
            """);

        await context.MigrateAsync(Migration);

        (await ColumnCountAsync(context, "AspNetUsers", "RefreshToken")).Should().Be(0);
        (await ColumnCountAsync(context, "AspNetUsers", "RefreshTokenExpiryTime")).Should().Be(0);
        (await context.ScalarAsync<Guid>("SELECT Id FROM AspNetUsers")).Should().Be(userId);
        (await context.ScalarAsync<int>("SELECT COUNT(*) FROM RefreshToken")).Should().Be(0, "old tokens are not carried over");
    }

    [Fact]
    public async Task Migrate_TokenHashesAreUniqueAndBelongToAUser()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(Migration);
        var userId = Guid.NewGuid();
        await context.ExecuteAsync($"""
            INSERT INTO AspNetUsers (Id, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES ('{userId}', 1, 0, 0, 1, 0);
            {InsertToken(userId, "hash")}
            """);

        var duplicateHash = () => context.ExecuteAsync(InsertToken(userId, "hash"));
        var unknownUser = () => context.ExecuteAsync(InsertToken(Guid.NewGuid(), "other-hash"));

        await duplicateHash.Should().ThrowAsync<SqlException>().WithMessage("*IX_RefreshToken_TokenHash*");
        await unknownUser.Should().ThrowAsync<SqlException>().WithMessage("*FK_RefreshToken_AspNetUsers_UserId*");
    }

    private static string InsertToken(Guid userId, string hash) => $"""
        INSERT INTO RefreshToken (Id, UserId, TokenHash, FamilyId, ExpiresAt, CreatedOn, UpdatedOn)
        VALUES ('{Guid.NewGuid()}', '{userId}', '{hash}', '{Guid.NewGuid()}', '2026-01-08', '2026-01-01', '2026-01-01');
        """;

    private static Task<int> ColumnCountAsync(InsequensContext context, string table, string column) =>
        context.ScalarAsync<int>(
            $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'");
}
