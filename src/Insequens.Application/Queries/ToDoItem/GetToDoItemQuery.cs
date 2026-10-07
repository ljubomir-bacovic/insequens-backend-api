using Insequens.Contracts.V1.Tasks;
using MediatR;

namespace Insequens.Application.Queries.ToDoItem;

/// <summary>Not <c>IOwned</c>: the handler's owner filter is the authorization, in the same single query.</summary>
public record GetToDoItemQuery(Guid ItemId, Guid UserId) : IRequest<Versioned<ToDoItemGetDetailsModel>>;
