using FluentAssertions;
using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Insequens.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Authorization;

public sealed class AuthorizationBehaviorTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithOwnedItem_GivesTheHandlerTheLoadedItem()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, name: "Owned");

        var name = await SendAsync(new ReadToDoItemName(userId, item.Id));

        name.Should().Be("Owned");
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsNotFoundExceptionBeforeTheHandler()
    {
        var itemId = Guid.NewGuid();

        var action = () => SendAsync(new ReadToDoItemName(Guid.NewGuid(), itemId));

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.Id.Should().Be(itemId);
        exception.Which.ResourceName.Should().Be("ToDoItem");
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsNotFoundExceptionBeforeTheHandler()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => SendAsync(new ReadToDoItemName(Guid.NewGuid(), item.Id));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task Send_ForAnotherEntityType_UsesThatEntitysPolicy()
    {
        var ownerId = Guid.NewGuid();
        var note = new Note(Guid.NewGuid(), ownerId, "Note text");
        var policy = new InMemoryNotePolicy(note);
        await using var services = BuildServices(policy);
        var mediator = services.GetRequiredService<IMediator>();

        var text = await mediator.Send(new ReadNoteText(ownerId, note.Id));
        var otherUser = () => mediator.Send(new ReadNoteText(Guid.NewGuid(), note.Id));

        text.Should().Be("Note text");
        (await otherUser.Should().ThrowAsync<NotFoundException>()).Which.ResourceName.Should().Be(nameof(Note));
    }

    [Fact]
    public async Task Send_ForRequestWithoutIOwned_DoesNotAuthorize()
    {
        var policy = new InMemoryNotePolicy();
        await using var services = BuildServices(policy);

        var result = await services.GetRequiredService<IMediator>().Send(new Unowned(Guid.NewGuid()));

        result.Should().Be("handled");
        policy.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Resource_WhenNothingWasAuthorized_ThrowsInvalidOperationException()
    {
        await using var services = BuildServices(new InMemoryNotePolicy());
        var context = services.GetRequiredService<IResourceContext<Note>>();

        var action = () => context.Resource;

        action.Should().Throw<InvalidOperationException>().WithMessage("*IOwned<Note>*");
    }

    private Task<string> SendAsync(ReadToDoItemName request) =>
        _database.SendAsync(request, services =>
            services.AddTransient<IRequestHandler<ReadToDoItemName, string>, ReadToDoItemNameHandler>());

    private static ServiceProvider BuildServices(InMemoryNotePolicy policy)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IApplicationDbContext>());
        services.AddApplication();
        services.AddSingleton<IOwnershipPolicy<Note>>(policy);
        services.AddTransient<IRequestHandler<ReadNoteText, string>, ReadNoteTextHandler>();
        services.AddTransient<IRequestHandler<Unowned, string>, UnownedHandler>();

        return services.BuildServiceProvider();
    }

    public sealed record ReadToDoItemName(Guid UserId, Guid ResourceId) : IRequest<string>, IOwned<ToDoItemEntity>;

    public sealed class ReadToDoItemNameHandler(IResourceContext<ToDoItemEntity> item) : IRequestHandler<ReadToDoItemName, string>
    {
        public Task<string> Handle(ReadToDoItemName request, CancellationToken cancellationToken) =>
            Task.FromResult(item.Resource.Name);
    }

    public sealed record Note(Guid Id, Guid UserId, string Text) : IOwnedEntity;

    public sealed record ReadNoteText(Guid UserId, Guid ResourceId) : IRequest<string>, IOwned<Note>;

    public sealed record Unowned(Guid UserId) : IRequest<string>;

    private sealed class ReadNoteTextHandler(IResourceContext<Note> note) : IRequestHandler<ReadNoteText, string>
    {
        public Task<string> Handle(ReadNoteText request, CancellationToken cancellationToken) =>
            Task.FromResult(note.Resource.Text);
    }

    private sealed class UnownedHandler : IRequestHandler<Unowned, string>
    {
        public Task<string> Handle(Unowned request, CancellationToken cancellationToken) => Task.FromResult("handled");
    }

    private sealed class InMemoryNotePolicy(params Note[] notes) : IOwnershipPolicy<Note>
    {
        public int Calls { get; private set; }

        public Task<Note?> FindOwnedAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(notes.SingleOrDefault(note => note.Id == resourceId && note.UserId == userId));
        }
    }
}
