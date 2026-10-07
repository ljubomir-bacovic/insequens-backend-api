using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemNameHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemNameCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemNameCommand request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .SingleOrDefaultAsync(toDoItem => toDoItem.Id == request.ItemId, cancellationToken)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        item.Rename(request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
