using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using Insequens.Application.Commands;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class DeleteToDoItemHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<DeleteToDoItemCommand>
{
    public async Task Handle(
        DeleteToDoItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        item.Delete();
        await dbContext.SaveChangesAsync(item, request.ExpectedVersion, cancellationToken);
    }
}
