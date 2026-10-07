using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using Insequens.Contracts.V1.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Queries.ToDoItem;

public class GetToDoItemHandler(IApplicationDbContext dbContext, IMapper mapper)
    : IRequestHandler<GetToDoItemQuery, Versioned<ToDoItemGetDetailsModel>>
{
    public async Task<Versioned<ToDoItemGetDetailsModel>> Handle(
        GetToDoItemQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.ToDoItems
            .AsNoTracking()
            .Where(item => item.Id == request.ItemId && item.UserId == request.UserId)
            .ProjectTo<Versioned<ToDoItemGetDetailsModel>>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(typeof(ToDoItemEntity).Name, request.ItemId);
    }
}
