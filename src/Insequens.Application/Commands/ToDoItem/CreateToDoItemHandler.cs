using Insequens.Domain.DataAccess;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class CreateToDoItemHandler(IDataContext dataContext)
    : IRequestHandler<CreateToDoItemCommand, ToDoItemGetDetailsModel>
{
    public async Task<ToDoItemGetDetailsModel> Handle(
        CreateToDoItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = new ToDoItemEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            Description = request.Description,
            Priority = (TaskPriority?)request.Priority,
            DueDate = request.DueDate,
            IsCompleted = false,
        };

        dataContext.GetRepository<ToDoItemEntity>().AddOrUpdate(item);
        await dataContext.SaveChangesAsync(cancellationToken);

        return new ToDoItemGetDetailsModel(
            item.Id,
            item.Name,
            item.Description,
            item.Priority,
            item.DueDate,
            item.IsCompleted);
    }
}
