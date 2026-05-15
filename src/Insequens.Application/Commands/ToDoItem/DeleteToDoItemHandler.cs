using Insequens.Domain.DataAccess;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class DeleteToDoItemHandler(IDataContext dataContext)
    : IRequestHandler<DeleteToDoItemCommand, Unit>
{
    public async Task<Unit> Handle(DeleteToDoItemCommand request, CancellationToken cancellationToken)
    {
        var repository = dataContext.GetRepository<ToDoItemEntity>();
        var item = (await repository.FindAsync(request.ItemId))!;
        repository.Remove(item);
        await dataContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
