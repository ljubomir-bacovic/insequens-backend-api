using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemDescriptionHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemDescriptionCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemDescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .SingleOrDefaultAsync(toDoItem => toDoItem.Id == request.ItemId, cancellationToken)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        item.UpdateDescription(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
