using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemDueDateHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemDueDateCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemDueDateCommand request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .SingleOrDefaultAsync(toDoItem => toDoItem.Id == request.ItemId, cancellationToken)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        item.DueDate = request.DueDate;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
