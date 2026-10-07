using FluentAssertions;
using Insequens.Application.Authorization;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Queries.Tasks;
using Insequens.Application.Queries.ToDoItem;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Authorization;

public class OwnedRequestTests
{
    /// <summary>Requests that address an item by ID but authorize in their own query, filtered by owner.</summary>
    private static readonly Type[] OwnerFilteredQueries = [typeof(GetToDoItemQuery), typeof(GetTaskQuery)];

    [Fact]
    public void RequestsAddressingAnItem_AreOwnedOrFilterByOwner()
    {
        var unprotected = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type.GetInterfaces().Any(IsMediatRRequest))
            .Where(type => type.GetProperty(nameof(UpdateToDoItemNameCommand.ItemId)) is not null)
            .Where(type => !typeof(IOwned<ToDoItemEntity>).IsAssignableFrom(type))
            .Except(OwnerFilteredQueries);

        unprotected.Should().BeEmpty("a request that addresses an item by ID must implement IOwned<ToDoItem>");
    }

    [Fact]
    public void OwnedRequest_ExposesItemIdAsResourceId()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        IResourceRequest request = new UpdateToDoItemNameCommand(itemId, userId, "Name");

        request.ResourceId.Should().Be(itemId);
        request.UserId.Should().Be(userId);
    }

    private static bool IsMediatRRequest(Type type) =>
        type == typeof(IRequest) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>));
}
