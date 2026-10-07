using FluentAssertions;
using Insequens.Application.Abstractions;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Persistence;
using Insequens.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Insequens.Infrastructure.Tests.Persistence;

public class AuditableEntityInterceptorTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(StartTime);
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedAndUpdatedFields()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        await using var context = CreateContext();
        var item = NewItem();

        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
        item.CreatedOn.Kind.Should().Be(DateTimeKind.Utc);
        item.UpdatedOn.Kind.Should().Be(DateTimeKind.Utc);
        item.CreatedBy.Should().Be(userId);
        item.UpdatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_UpdatesOnlyUpdatedFields()
    {
        var creator = Guid.NewGuid();
        var editor = Guid.NewGuid();
        _currentUser.UserId.Returns(creator);
        await using var context = CreateContext();
        var item = NewItem();
        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        _timeProvider.Advance(TimeSpan.FromHours(2));
        _currentUser.UserId.Returns(editor);
        item.Rename("Renamed");
        await context.SaveChangesAsync(CancellationToken.None);

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.CreatedBy.Should().Be(creator);
        item.UpdatedOn.Should().Be(StartTime.AddHours(2).UtcDateTime);
        item.UpdatedBy.Should().Be(editor);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityUnchanged_LeavesAuditFields()
    {
        await using var context = CreateContext();
        var item = NewItem();
        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        _timeProvider.Advance(TimeSpan.FromHours(2));
        await context.SaveChangesAsync(CancellationToken.None);

        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutCurrentUser_LeavesActorsEmpty()
    {
        _currentUser.UserId.Returns((Guid?)null);
        await using var context = CreateContext();
        var item = NewItem();

        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        item.CreatedBy.Should().BeNull();
        item.UpdatedBy.Should().BeNull();
        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
    }

    [Fact]
    public void SaveChanges_WhenEntityAdded_SetsAuditFields()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        using var context = CreateContext();
        var item = NewItem();

        context.ToDoItems.Add(item);
        context.SaveChanges();

        item.CreatedOn.Should().Be(StartTime.UtcDateTime);
        item.UpdatedOn.Should().Be(StartTime.UtcDateTime);
        item.CreatedBy.Should().Be(userId);
    }

    private static ToDoItem NewItem() => ToDoItem.Create(Guid.NewGuid(), "Task", null, null, null);

    private InsequensContext CreateContext() => new(
        new DbContextOptionsBuilder<InsequensContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(_timeProvider, _currentUser))
            .Options);
}
