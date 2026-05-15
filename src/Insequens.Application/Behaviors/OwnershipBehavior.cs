using Insequens.Application.Commands;
using Insequens.Application.Exceptions;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using MediatR;

namespace Insequens.Application.Behaviors;

public class OwnershipBehavior<TRequest, TResponse>(IDataContext dataContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IOwned
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var repository = dataContext.GetRepository<ToDoItem>();
        var item = await repository.FindAsync(request.ItemId)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        if (item.UserId != request.UserId)
        {
            throw new ResourceForbiddenException(request.ItemId);
        }

        return await next(cancellationToken);
    }
}
