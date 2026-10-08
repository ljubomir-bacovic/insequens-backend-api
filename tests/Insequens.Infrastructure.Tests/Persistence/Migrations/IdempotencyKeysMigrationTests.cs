using FluentAssertions;
using Insequens.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Insequens.Infrastructure.Tests.Persistence.Migrations;

[Collection(SqlServerCollection.Name)]
public sealed class IdempotencyKeysMigrationTests(SqlServerFixture sqlServer)
{
    private const string Migration = "20261008082554_IdempotencyKeys";

    private static readonly Guid UserId = Guid.Parse("4d0b2e3f-6a7c-4d8e-9f0a-3b4c5d6e7f80");

    [Fact]
    public async Task Migrate_TheDatabaseRejectsTheSameKeyTwiceForOneUserOnly()
    {
        await using var context = sqlServer.CreateContext();
        await context.MigrateAsync(Migration);
        var otherUser = Guid.NewGuid();
        await context.ExecuteAsync(InsertUser(UserId) + InsertUser(otherUser) + InsertKey(UserId, "key-1"));

        var sameUser = () => context.ExecuteAsync(InsertKey(UserId, "key-1"));
        var otherUsersKey = () => context.ExecuteAsync(InsertKey(otherUser, "key-1"));

        await sameUser.Should().ThrowAsync<SqlException>().WithMessage("*IX_IdempotencyKeys_UserId_Key*");
        await otherUsersKey.Should().NotThrowAsync();
    }

    private static string InsertUser(Guid userId) => $"""
        INSERT INTO AspNetUsers (Id, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES ('{userId}', 1, 0, 0, 1, 0);
        """;

    private static string InsertKey(Guid userId, string key) => $"""
        INSERT INTO IdempotencyKeys (Id, UserId, [Key], RequestHash, ExpiresAt, CreatedOn, UpdatedOn)
        VALUES (NEWID(), '{userId}', '{key}', REPLICATE('A', 64), '2026-01-02', '2026-01-01', '2026-01-01');
        """;
}
