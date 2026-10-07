using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemDescriptionHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemDescriptionCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemDescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        item.UpdateDescription(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
