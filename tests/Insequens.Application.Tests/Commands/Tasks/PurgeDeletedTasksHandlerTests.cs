using FluentAssertions;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Options;
using Insequens.Application.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application.Tests.Commands.Tasks;

public sealed class PurgeDeletedTasksHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_RemovesOnlyTasksDeletedLongerThanTheRetention()
    {
        var expired = await SeedDeletedAsync("Deleted 31 days ago");
        _database.Clock.Advance(TimeSpan.FromDays(2));
        var recent = await SeedDeletedAsync("Deleted 29 days ago");
        var live = await _database.SeedItemAsync(_userId, "Never deleted");
        _database.Clock.Advance(TimeSpan.FromDays(29));

        var purged = await _database.SendAsync(new PurgeDeletedTasksCommand());

        purged.Should().Be(1);
        (await _database.FindItemAsync(expired)).Should().BeNull();
        (await _database.FindItemAsync(recent)).Should().NotBeNull();
        (await _database.FindItemAsync(live.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Send_UsesTheConfiguredRetention()
    {
        var deleted = await SeedDeletedAsync("Deleted 2 days ago");
        _database.Clock.Advance(TimeSpan.FromDays(2));

        var purged = await _database.SendAsync(new PurgeDeletedTasksCommand());
        var purgedWithOneDay = await _database.SendAsync(
            new PurgeDeletedTasksCommand(),
            services => services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new TaskTrashOptions { Retention = TimeSpan.FromDays(1) })));

        purged.Should().Be(0, "the default retention is 30 days");
        purgedWithOneDay.Should().Be(1);
        (await _database.FindItemAsync(deleted)).Should().BeNull();
    }

    private async Task<Guid> SeedDeletedAsync(string name)
    {
        var item = await _database.SeedItemAsync(_userId, name);
        await _database.SendAsync(new DeleteTaskCommand(item.Id, _userId));

        return item.Id;
    }
}
