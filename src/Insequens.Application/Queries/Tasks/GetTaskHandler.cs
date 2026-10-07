using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using Insequens.Contracts.V2.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Queries.Tasks;

public class GetTaskHandler(IApplicationDbContext dbContext, IMapper mapper)
    : IRequestHandler<GetTaskQuery, Versioned<TaskResponse>>
{
    public async Task<Versioned<TaskResponse>> Handle(GetTaskQuery request, CancellationToken cancellationToken) =>
        await dbContext.ToDoItems
            .AsNoTracking()
            .Where(item => item.Id == request.ItemId && item.UserId == request.UserId)
            .ProjectTo<Versioned<TaskResponse>>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(typeof(ToDoItemEntity).Name, request.ItemId);
}
