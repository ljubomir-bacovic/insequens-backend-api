using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Commands.ToDoItem;

public class DeleteToDoItemHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteToDoItemCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteToDoItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .SingleOrDefaultAsync(toDoItem => toDoItem.Id == request.ItemId, cancellationToken)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        dbContext.ToDoItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
