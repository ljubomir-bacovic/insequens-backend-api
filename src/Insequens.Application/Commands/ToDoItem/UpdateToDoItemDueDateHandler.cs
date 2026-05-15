using Insequens.Domain.DataAccess;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemDueDateHandler(IDataContext dataContext)
    : IRequestHandler<UpdateToDoItemDueDateCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemDueDateCommand request,
        CancellationToken cancellationToken)
    {
        var repository = dataContext.GetRepository<ToDoItemEntity>();
        var item = (await repository.FindAsync(request.ItemId))!;
        item.DueDate = request.DueDate;
        await dataContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
