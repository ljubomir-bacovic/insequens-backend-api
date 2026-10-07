using Insequens.Application.Abstractions;
using Insequens.Application.Profiles;
using Insequens.Contracts.V1.Tasks;
using MediatR;
using DomainPriority = Insequens.Domain.Types.TaskPriority;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class CreateToDoItemHandler(IApplicationDbContext dbContext)
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
            Priority = (DomainPriority?)request.Priority,
            DueDate = request.DueDate,
            IsCompleted = false,
        };

        dbContext.ToDoItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ToDoItemGetDetailsModel(
            item.Id,
            item.Name,
            item.Description,
            item.Priority.ToContract(),
            item.DueDate,
            item.IsCompleted);
    }
}
