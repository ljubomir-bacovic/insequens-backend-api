using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemPriorityHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemPriorityCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemPriorityCommand request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .SingleOrDefaultAsync(toDoItem => toDoItem.Id == request.ItemId, cancellationToken)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        item.Priority = request.Priority;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
