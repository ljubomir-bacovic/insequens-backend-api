using AutoMapper;
using Insequens.Application.Abstractions;
using Insequens.Contracts.V2.Tasks;
using MediatR;
using DomainPriority = Insequens.Domain.Types.TaskPriority;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.Tasks;

public class CreateTaskHandler(IApplicationDbContext dbContext, IMapper mapper)
    : IRequestHandler<CreateTaskCommand, TaskResponse>
{
    public async Task<TaskResponse> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var item = ToDoItemEntity.Create(
            request.UserId,
            request.Name,
            request.Description,
            (DomainPriority)request.Priority,
            request.DueDate);

        dbContext.ToDoItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return mapper.Map<TaskResponse>(item);
    }
}
