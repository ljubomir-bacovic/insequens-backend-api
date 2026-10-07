using Insequens.Application.Abstractions;
using Insequens.Application.Profiles;
using Insequens.Contracts.V1.Tasks;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class CreateToDoItemHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateToDoItemCommand, ToDoItemGetDetailsModel>
{
    public async Task<ToDoItemGetDetailsModel> Handle(
        CreateToDoItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = ToDoItemEntity.Create(
            request.UserId,
            request.Name,
            request.Description,
            TaskPriorityConversions.FromV1Value(request.Priority),
            request.DueDate);

        dbContext.ToDoItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ToDoItemGetDetailsModel(
            item.Id,
            item.Name,
            item.Description,
            item.Priority.ToV1(),
            item.DueDate,
            item.IsCompleted);
    }
}
