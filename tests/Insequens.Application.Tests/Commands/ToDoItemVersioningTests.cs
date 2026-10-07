using FluentAssertions;
using Insequens.Application.Abstractions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insequens.Application.Tests.Commands;

public sealed class ToDoItemVersioningTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithTheCurrentVersion_SavesTheChange()
    {
        var item = await _database.SeedItemAsync(_userId, "Original");

        await _database.SendAsync(new UpdateToDoItemNameCommand(item.Id, _userId, "Renamed", item.RowVersion));

        (await _database.FindItemAsync(item.Id))!.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task Send_WithAStaleVersion_ThrowsPreconditionFailedAndKeepsTheItem()
    {
        var item = await _database.SeedItemAsync(_userId, "Original");

        var rename = () => _database.SendAsync(new UpdateToDoItemNameCommand(item.Id, _userId, "Renamed", [1, 2, 3]));
        var delete = () => _database.SendAsync(new DeleteToDoItemCommand(item.Id, _userId, [1, 2, 3]));

        (await rename.Should().ThrowAsync<PreconditionFailedException>()).Which.Id.Should().Be(item.Id);
        await delete.Should().ThrowAsync<PreconditionFailedException>();
        (await _database.FindItemAsync(item.Id))!.Name.Should().Be("Original");
    }

    [Fact]
    public async Task Send_WhenAnotherRequestSavesFirstAndNoVersionWasGiven_ThrowsConcurrencyConflict()
    {
        var item = await _database.SeedItemAsync(_userId, "Original");

        var rename = () => SendWithConcurrentChangeAsync(new UpdateToDoItemNameCommand(item.Id, _userId, "Renamed"), item.Id);

        (await rename.Should().ThrowAsync<ConcurrencyConflictException>()).Which.Id.Should().Be(item.Id);
        (await _database.FindItemAsync(item.Id))!.Name.Should().Be("Original");
    }

    [Fact]
    public async Task Send_WhenAnotherRequestSavesFirstAfterTheVersionCheck_ThrowsPreconditionFailed()
    {
        var item = await _database.SeedItemAsync(_userId, "Original");

        var delete = () => SendWithConcurrentChangeAsync(new DeleteToDoItemCommand(item.Id, _userId, item.RowVersion), item.Id);

        await delete.Should().ThrowAsync<PreconditionFailedException>();
        (await _database.FindItemAsync(item.Id)).Should().NotBeNull();
    }

    private Task SendWithConcurrentChangeAsync(MediatR.IRequest command, Guid itemId)
    {
        var concurrentChange = new ChangeVersionBeforeSaveInterceptor(_database, itemId);

        return _database.SendAsync(command, services =>
            services.Replace(ServiceDescriptor.Scoped<IApplicationDbContext>(_ => _database.CreateContext(concurrentChange))));
    }

    /// <summary>Gives the row a new version from a second connection just before the handler saves, as a parallel request would.</summary>
    private sealed class ChangeVersionBeforeSaveInterceptor(TestDbContextFactory database, Guid itemId) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            await other.Database.ExecuteSqlAsync(
                $"UPDATE Tasks SET RowVersion = randomblob(8) WHERE Id = {itemId}",
                cancellationToken);

            return result;
        }
    }
}
