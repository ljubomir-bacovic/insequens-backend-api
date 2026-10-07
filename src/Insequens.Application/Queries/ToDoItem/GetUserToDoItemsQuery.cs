using MediatR;
using Insequens.Contracts.V1.Tasks;
using Insequens.Contracts.V1;

namespace Insequens.Application.Queries.ToDoItem;

public record GetUserToDoItemsQuery(
    Guid UserId,
    bool IsCompleted,
    int Page,
    int PageSize) : IRequest<PaginatedResult<ToDoItemGetListModel>>;
