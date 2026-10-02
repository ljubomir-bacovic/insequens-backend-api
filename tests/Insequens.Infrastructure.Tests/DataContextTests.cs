using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Insequens.Domain.Data;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Infrastructure.Tests;

public class DataContextTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 29, 1, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ModifiedAt = new(2026, 3, 29, 4, 45, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedOnAndUpdatedOnFromTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(CreatedAt);
        await using var context = CreateContext();
        using var dataContext = new DataContext(context, timeProvider);
        var item = CreateItem();

        dataContext.GetRepository<ToDoItem>().AddOrUpdate(item, isNew: true);
        await dataContext.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(CreatedAt.UtcDateTime);
        item.UpdatedOn.Should().Be(CreatedAt.UtcDateTime);
        item.CreatedOn.Kind.Should().Be(DateTimeKind.Utc);
        item.UpdatedOn.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_UpdatesOnlyUpdatedOn()
    {
        var timeProvider = new FakeTimeProvider(CreatedAt);
        await using var context = CreateContext();
        using var dataContext = new DataContext(context, timeProvider);
        var item = CreateItem();
        dataContext.GetRepository<ToDoItem>().AddOrUpdate(item, isNew: true);
        await dataContext.SaveChangesAsync(CancellationToken.None);

        timeProvider.SetUtcNow(ModifiedAt);
        item.Name = "Renamed";
        await dataContext.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(CreatedAt.UtcDateTime);
        item.UpdatedOn.Should().Be(ModifiedAt.UtcDateTime);
        item.UpdatedOn.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SaveChanges_WhenEntityAdded_SetsCreatedOnAndUpdatedOnFromTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(CreatedAt);
        using var context = CreateContext();
        using var dataContext = new DataContext(context, timeProvider);
        var item = CreateItem();

        dataContext.GetRepository<ToDoItem>().AddOrUpdate(item, isNew: true);
        dataContext.SaveChanges();

        item.CreatedOn.Should().Be(CreatedAt.UtcDateTime);
        item.UpdatedOn.Should().Be(CreatedAt.UtcDateTime);
    }

    [Fact]
    public void Constructor_WhenTimeProviderIsNull_ThrowsArgumentNullException()
    {
        using var context = CreateContext();

        var act = () => new DataContext(context, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    private static ToDoItem CreateItem() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Audited item",
    };

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
