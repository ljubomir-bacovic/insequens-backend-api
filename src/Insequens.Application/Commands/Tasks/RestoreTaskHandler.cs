using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.Tasks;

public class RestoreTaskHandler(IApplicationDbContext dbContext) : IRequestHandler<RestoreTaskCommand>
{
    public async Task Handle(RestoreTaskCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == request.ItemId && item.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(typeof(ToDoItemEntity).Name, request.ItemId);

        item.Restore();
        await dbContext.SaveChangesAsync(item, request.ExpectedVersion, cancellationToken);
    }
}
