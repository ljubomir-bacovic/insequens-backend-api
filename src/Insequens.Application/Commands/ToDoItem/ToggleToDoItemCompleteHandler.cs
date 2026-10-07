using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Commands.ToDoItem;

public class ToggleToDoItemCompleteHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ToggleToDoItemCompleteCommand, Unit>
{
    public async Task<Unit> Handle(
        ToggleToDoItemCompleteCommand request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .SingleOrDefaultAsync(toDoItem => toDoItem.Id == request.ItemId, cancellationToken)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        item.IsCompleted = !item.IsCompleted;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
