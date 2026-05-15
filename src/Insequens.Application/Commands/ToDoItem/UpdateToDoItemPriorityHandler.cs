using Insequens.Domain.DataAccess;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemPriorityHandler(IDataContext dataContext)
    : IRequestHandler<UpdateToDoItemPriorityCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemPriorityCommand request,
        CancellationToken cancellationToken)
    {
        var repository = dataContext.GetRepository<ToDoItemEntity>();
        var item = (await repository.FindAsync(request.ItemId))!;
        item.Priority = request.Priority;
        await dataContext.SaveChangesAsync();
        return Unit.Value;
    }
}
