using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Insequens.Contracts.V1.Tasks;

namespace Insequens.Application.Queries.ToDoItem;

public class GetToDoItemHandler(IApplicationDbContext dbContext, IMapper mapper)
    : IRequestHandler<GetToDoItemQuery, ToDoItemGetDetailsModel>
{
    public async Task<ToDoItemGetDetailsModel> Handle(
        GetToDoItemQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.ToDoItems
            .AsNoTracking()
            .Where(item => item.Id == request.ItemId)
            .ProjectTo<ToDoItemGetDetailsModel>(mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
