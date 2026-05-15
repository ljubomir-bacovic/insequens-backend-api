using Insequens.Domain.DataAccess;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemDescriptionHandler(IDataContext dataContext)
    : IRequestHandler<UpdateToDoItemDescriptionCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateToDoItemDescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var repository = dataContext.GetRepository<ToDoItemEntity>();
        var item = (await repository.FindAsync(request.ItemId))!;
        item.Description = request.Description;
        await dataContext.SaveChangesAsync();
        return Unit.Value;
    }
}
