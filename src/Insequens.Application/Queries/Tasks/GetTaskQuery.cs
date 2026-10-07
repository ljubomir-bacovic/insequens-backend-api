using Insequens.Contracts.V2.Tasks;
using MediatR;

namespace Insequens.Application.Queries.Tasks;

/// <summary>Not <c>IOwned</c>: the handler's owner filter is the authorization, in the same single query.</summary>
public record GetTaskQuery(Guid ItemId, Guid UserId) : IRequest<Versioned<TaskResponse>>;
