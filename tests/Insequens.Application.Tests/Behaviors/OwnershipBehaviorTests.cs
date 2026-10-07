using FluentAssertions;
using Insequens.Application.Behaviors;
using Insequens.Application.Commands;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using MediatR;

namespace Insequens.Application.Tests.Behaviors;

public sealed class OwnershipBehaviorTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private bool _nextCalled;

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Handle_WithOwnedItem_CallsNext()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId);

        var result = await HandleAsync(new TestOwnedRequest(userId, item.Id));

        result.Should().Be(Unit.Value);
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonexistentItem_ThrowsToDoItemNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => HandleAsync(new TestOwnedRequest(Guid.NewGuid(), itemId));

        (await action.Should().ThrowAsync<ToDoItemNotFoundException>()).Which.Id.Should().Be(itemId);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithOtherUsersItem_ThrowsResourceForbiddenException()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => HandleAsync(new TestOwnedRequest(Guid.NewGuid(), item.Id));

        (await action.Should().ThrowAsync<ResourceForbiddenException>()).Which.Id.Should().Be(item.Id);
        _nextCalled.Should().BeFalse();
    }

    private async Task<Unit> HandleAsync(TestOwnedRequest request)
    {
        await using var context = _database.CreateContext();
        var behavior = new OwnershipBehavior<TestOwnedRequest, Unit>(context);

        return await behavior.Handle(request, _ =>
        {
            _nextCalled = true;
            return Task.FromResult(Unit.Value);
        }, CancellationToken.None);
    }

    private sealed record TestOwnedRequest(Guid UserId, Guid ItemId) : IRequest<Unit>, IOwned;
}
