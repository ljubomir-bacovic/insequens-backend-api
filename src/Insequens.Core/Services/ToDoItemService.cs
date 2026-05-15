using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Exceptions;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.ServiceContracts;
using Insequens.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Core.Services;

public class ToDoItemService : IToDoItemService
{
    IDataContext _dataContext;
    IRepository<ToDoItem> _toDoItemRepository;
    private readonly IMapper _mapper;
    public ToDoItemService(IDataContext dataContext, IMapper mapper)
    {
        _dataContext = dataContext;
        _mapper = mapper;
        _toDoItemRepository = dataContext.GetRepository<ToDoItem>();
    }
    public async Task<ToDoItemGetDetailsModel> AddToDoItemAsync(ToDoItemCreateModel toDoItemCreate, Guid userId)
    {

        var toDoItem = _mapper.Map<ToDoItem>(toDoItemCreate, 
            opt => opt.AfterMap((dest, src) => src.UserId = userId));

        _toDoItemRepository.AddOrUpdate(toDoItem);
        await _dataContext.SaveChangesAsync();
        return _mapper.Map<ToDoItemGetDetailsModel>(toDoItem);
    }

    public async Task<List<ToDoItemGetListModel>> GetUserToDoItemsAsync(Guid userId, bool isCompleted, int page, int pageSize)
    {
        return await _toDoItemRepository.AsQueryable()
            .Where(x => x.UserId == userId && x.IsCompleted == isCompleted)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Priority)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ProjectTo<ToDoItemGetListModel>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    private async Task<ToDoItem> GetOwnedItemAsync(Guid id, Guid userId)
    {
        var toDoItem = await _toDoItemRepository.FindAsync(id)
            ?? throw new ToDoItemNotFoundException(id);
        if (toDoItem.UserId != userId)
            throw new ResourceForbiddenException(id);
        return toDoItem;
    }

    public async Task UpdateToDoItemAsync(ToDoItemUpdateModel toDoItemUpdate, Guid userId)
    {
        var toDoItem = await GetOwnedItemAsync(toDoItemUpdate.Id, userId);
        _mapper.Map(toDoItemUpdate, toDoItem);

        _toDoItemRepository.AddOrUpdate(toDoItem);
        await _dataContext.SaveChangesAsync();
    }

    public async Task DeleteToDoItemAsync(Guid id, Guid userId)
    {
        var toDoItem = await GetOwnedItemAsync(id, userId);
        _toDoItemRepository.Remove(toDoItem);
        await _dataContext.SaveChangesAsync();
    }

    public async Task<ToDoItemGetDetailsModel> GetToDoItem(Guid id, Guid userId)
    {
        var toDoItem = await GetOwnedItemAsync(id, userId);
        return _mapper.Map<ToDoItemGetDetailsModel>(toDoItem);
    }

    public async Task ToggleToDoItemCompleteAsync(Guid id, Guid userId)
    {
        var toDoItem = await GetOwnedItemAsync(id, userId);
        toDoItem.IsCompleted = !toDoItem.IsCompleted;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemPriorityAsync(Guid id, Guid userId, TaskPriority priority)
    {
        var toDoItem = await GetOwnedItemAsync(id, userId);
        toDoItem.Priority = priority;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemNameAsync(Guid id, Guid userId, string name)
    {
        var toDoItem = await GetOwnedItemAsync(id, userId);
        toDoItem.Name = name;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemDescriptionAsync(Guid id, Guid userId, string description)
    {
        var toDoItem = await GetOwnedItemAsync(id, userId);
        toDoItem.Description = description;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemDueDateAsync(Guid id, Guid userId, DateOnly date)
    {
        var toDoItem = await GetOwnedItemAsync(id, userId);
        toDoItem.DueDate = date;

        await _dataContext.SaveChangesAsync();
    }
}
