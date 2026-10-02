using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Insequens.Domain.Data;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Infrastructure.Tests.DataAccess;

public class DataContextTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedOnAndUpdatedOnFromTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var context = CreateContext();
        using var dataContext = new DataContext(context, timeProvider);
        var item = new ToDoItem { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Task" };

        dataContext.GetRepository<ToDoItem>().AddOrUpdate(item);
        await dataContext.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
        item.CreatedOn.Kind.Should().Be(DateTimeKind.Utc);
        item.UpdatedOn.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_UpdatesOnlyUpdatedOn()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var context = CreateContext();
        using var dataContext = new DataContext(context, timeProvider);
        var item = new ToDoItem { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Task" };
        dataContext.GetRepository<ToDoItem>().AddOrUpdate(item);
        await dataContext.SaveChangesAsync(CancellationToken.None);

        timeProvider.Advance(TimeSpan.FromHours(2));
        item.Name = "Renamed";
        await dataContext.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.AddHours(2).UtcDateTime);
    }

    [Fact]
    public async Task SaveChanges_WhenEntityAdded_SetsAuditTimestampsFromTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var context = CreateContext();
        using var dataContext = new DataContext(context, timeProvider);
        var item = new ToDoItem { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Task" };

        dataContext.GetRepository<ToDoItem>().AddOrUpdate(item);
        dataContext.SaveChanges();

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
    }

    [Fact]
    public void Constructor_WhenTimeProviderIsNull_ThrowsArgumentNullException()
    {
        using var context = CreateContext();

        var action = () => new DataContext(context, null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    private static TestInsequensContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InsequensContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestInsequensContext(options);
    }

    private sealed class TestInsequensContext : InsequensContext
    {
        [SetsRequiredMembers]
        public TestInsequensContext(DbContextOptions<InsequensContext> options) : base(options)
        {
            ToDoItems = Set<ToDoItem>();
        }
    }
}
