using FluentAssertions;
using Insequens.Domain.Entities;

namespace Insequens.Domain.Tests;

public class IdempotencyRecordTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Begin_ClaimsTheKeyForTheRetentionWithoutAResponse()
    {
        var userId = Guid.NewGuid();

        var record = IdempotencyRecord.Begin(userId, "key", "HASH", Now);

        record.Should().BeEquivalentTo(new { UserId = userId, Key = "key", RequestHash = "HASH", ResponseBody = (string?)null, ExpiresAt = Now.AddHours(24) });
        record.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_IsTrueFromTheExpiryOn()
    {
        var record = IdempotencyRecord.Begin(Guid.NewGuid(), "key", "HASH", Now);

        record.IsExpired(Now.AddHours(24).AddTicks(-1)).Should().BeFalse();
        record.IsExpired(Now.AddHours(24)).Should().BeTrue();
    }

    [Fact]
    public void Matches_ComparesTheHashExactly()
    {
        var record = IdempotencyRecord.Begin(Guid.NewGuid(), "key", "HASH", Now);

        record.Matches("HASH").Should().BeTrue();
        record.Matches("hash").Should().BeFalse();
    }

    [Fact]
    public void Complete_StoresTheResponseAndRestartClearsIt()
    {
        var record = IdempotencyRecord.Begin(Guid.NewGuid(), "key", "HASH", Now);

        record.Complete("{}");
        record.IsCompleted.Should().BeTrue();

        record.Restart("OTHER", Now.AddDays(2));
        record.Should().BeEquivalentTo(new { RequestHash = "OTHER", ResponseBody = (string?)null, ExpiresAt = Now.AddDays(3) });
    }
}
