using Insequens.Application.Models;
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Queries.ToDoItem;

public record GetUserToDoItemsQuery(
    Guid UserId,
    bool IsCompleted,
    int Page,
    int PageSize) : IRequest<PaginatedResult<ToDoItemGetListModel>>;
