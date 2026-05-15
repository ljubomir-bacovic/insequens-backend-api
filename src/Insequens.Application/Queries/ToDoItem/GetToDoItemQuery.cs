using Insequens.Application.Commands;
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Queries.ToDoItem;

public record GetToDoItemQuery(Guid ItemId, Guid UserId)
    : IRequest<ToDoItemGetDetailsModel>, IOwned;
