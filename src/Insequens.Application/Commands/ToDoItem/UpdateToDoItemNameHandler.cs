using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemNameHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemNameCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemNameCommand request,
        CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        item.Rename(request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
