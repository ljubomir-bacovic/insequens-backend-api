using Insequens.Application.Exceptions;
using Insequens.Domain.DataAccess;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemNameHandler(IDataContext dataContext)
    : IRequestHandler<UpdateToDoItemNameCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemNameCommand request,
        CancellationToken cancellationToken)
    {
        var repository = dataContext.GetRepository<ToDoItemEntity>();
        var item = await repository.FindAsync(request.ItemId)
            ?? throw new ToDoItemNotFoundException(request.ItemId);
        item.Name = request.Name;
        await dataContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
