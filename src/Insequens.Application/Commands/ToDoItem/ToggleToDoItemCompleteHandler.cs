using Insequens.Domain.DataAccess;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class ToggleToDoItemCompleteHandler(IDataContext dataContext)
    : IRequestHandler<ToggleToDoItemCompleteCommand, Unit>
{
    public async Task<Unit> Handle(
        ToggleToDoItemCompleteCommand request,
        CancellationToken cancellationToken)
    {
        var repository = dataContext.GetRepository<ToDoItemEntity>();
        var item = (await repository.FindAsync(request.ItemId))!;
        item.IsCompleted = !item.IsCompleted;
        await dataContext.SaveChangesAsync();
        return Unit.Value;
    }
}
