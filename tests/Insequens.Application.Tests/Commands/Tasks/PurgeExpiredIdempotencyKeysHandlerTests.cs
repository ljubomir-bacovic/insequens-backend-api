using FluentAssertions;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V2.Tasks;

namespace Insequens.Application.Tests.Commands.Tasks;

public sealed class PurgeExpiredIdempotencyKeysHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_DeletesOnlyExpiredKeysAndKeepsTheirTasks()
    {
        await _database.SeedUserAsync(_userId);
        await _database.SendAsync(new CreateTaskCommand(_userId, "Old", null, TaskPriority.None, null, "old"));
        _database.Clock.Advance(TimeSpan.FromHours(12));
        await _database.SendAsync(new CreateTaskCommand(_userId, "New", null, TaskPriority.None, null, "new"));
        _database.Clock.Advance(TimeSpan.FromHours(12));

        var purged = await _database.SendAsync(new PurgeExpiredIdempotencyKeysCommand());

        purged.Should().Be(1);
        (await _database.IdempotencyRecordsAsync(_userId)).Should().ContainSingle().Which.Key.Should().Be("new");
        (await _database.ItemsAsync(_userId)).Should().HaveCount(2);
    }
}
