using FluentAssertions;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Persistence;
using Insequens.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Infrastructure.Tests.Persistence;

public class AuditableEntityInterceptorTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedOnAndUpdatedOnFromTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var context = CreateContext(timeProvider);
        var item = NewItem();

        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
        item.CreatedOn.Kind.Should().Be(DateTimeKind.Utc);
        item.UpdatedOn.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_UpdatesOnlyUpdatedOn()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var context = CreateContext(timeProvider);
        var item = NewItem();
        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        timeProvider.Advance(TimeSpan.FromHours(2));
        item.Name = "Renamed";
        await context.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.AddHours(2).UtcDateTime);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityUnchanged_LeavesTimestamps()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var context = CreateContext(timeProvider);
        var item = NewItem();
        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        timeProvider.Advance(TimeSpan.FromHours(2));
        await context.SaveChangesAsync(CancellationToken.None);

        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
    }

    [Fact]
    public void SaveChanges_WhenEntityAdded_SetsAuditTimestampsFromTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        using var context = CreateContext(timeProvider);
        var item = NewItem();

        context.ToDoItems.Add(item);
        context.SaveChanges();

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
    }

    private static ToDoItem NewItem() => new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Task" };

    private static InsequensContext CreateContext(TimeProvider timeProvider) => new(
        new DbContextOptionsBuilder<InsequensContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(timeProvider))
            .Options);
}
