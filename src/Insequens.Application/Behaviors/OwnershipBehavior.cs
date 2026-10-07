using Insequens.Application.Commands;
using Insequens.Application.Exceptions;
using Insequens.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Behaviors;

public class OwnershipBehavior<TRequest, TResponse>(IApplicationDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IOwned
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.ToDoItems
            .AsNoTracking()
            .SingleOrDefaultAsync(toDoItem => toDoItem.Id == request.ItemId, cancellationToken)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        if (item.UserId != request.UserId)
        {
            throw new ResourceForbiddenException(request.ItemId);
        }

        return await next(cancellationToken);
    }
}
