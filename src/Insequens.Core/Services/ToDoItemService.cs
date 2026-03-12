using AutoMapper;
using AutoMapper.QueryableExtensions;
using Journal.Core.Exceptions;
using Journal.Domain.DataAccess;
using Journal.Domain.Entities;
using Journal.Domain.Model.ToDoItem;
using Journal.Domain.ServiceContracts;
using Journal.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace Journal.Core.Services;

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

    public async Task UpdateToDoItemAsync(ToDoItemUpdateModel toDoItemUpdate)
    {
        var toDoItem = _toDoItemRepository.Find(toDoItemUpdate.Id) ?? throw new ToDoItemNotFoundException(toDoItemUpdate.Id);
        _mapper.Map(toDoItemUpdate, toDoItem);

        _toDoItemRepository.AddOrUpdate(toDoItem);
        await _dataContext.SaveChangesAsync();
    }

    public async Task DeleteToDoItemAsync(Guid id)
    {
        var toDoItem = _toDoItemRepository.Find(id) ?? throw new ToDoItemNotFoundException(id);
        _toDoItemRepository.Remove(toDoItem);
        await _dataContext.SaveChangesAsync();
    }

    public async Task<ToDoItemGetDetailsModel> GetToDoItem(Guid id)
    {
        var toDoItem = await _toDoItemRepository.FindAsync(id) ?? throw new ToDoItemNotFoundException(id);
        return _mapper.Map<ToDoItemGetDetailsModel>(toDoItem);
    }

    public async Task ToggleToDoItemCompleteAsync(Guid id)
    {
        var toDoItem = await _toDoItemRepository.FindAsync(id) ?? throw new ToDoItemNotFoundException(id);
        toDoItem.IsCompleted = !toDoItem.IsCompleted;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemPriorityAsync(Guid id, TaskPriority priority)
    {
        var toDoItem = await _toDoItemRepository.FindAsync(id) ?? throw new ToDoItemNotFoundException(id);
        toDoItem.Priority = priority;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemNameAsync(Guid id, string name)
    {
        var toDoItem = await _toDoItemRepository.FindAsync(id) ?? throw new ToDoItemNotFoundException(id);
        toDoItem.Name = name;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemDescriptionAsync(Guid id, string description)
    {
        var toDoItem = await _toDoItemRepository.FindAsync(id) ?? throw new ToDoItemNotFoundException(id);
        toDoItem.Description = description;

        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateToDoItemDueDateAsync(Guid id, DateOnly date)
    {
        var toDoItem = await _toDoItemRepository.FindAsync(id) ?? throw new ToDoItemNotFoundException(id);
        toDoItem.DueDate = date;

        await _dataContext.SaveChangesAsync();
    }
}