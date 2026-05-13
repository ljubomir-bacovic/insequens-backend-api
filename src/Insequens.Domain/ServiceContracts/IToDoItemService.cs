using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;

namespace Insequens.Domain.ServiceContracts;

public interface IToDoItemService
{
    Task<ToDoItemGetDetailsModel> AddToDoItemAsync(ToDoItemCreateModel toDoItemCreate, Guid userId);
    Task<List<ToDoItemGetListModel>> GetUserToDoItemsAsync(Guid userId, bool isCompleted, int page, int pageSize);
    Task UpdateToDoItemAsync(ToDoItemUpdateModel toDoItemUpdate, Guid userId);
    Task DeleteToDoItemAsync(Guid id, Guid userId);
    Task<ToDoItemGetDetailsModel> GetToDoItem(Guid id, Guid userId);
    Task ToggleToDoItemCompleteAsync(Guid id, Guid userId);
    Task UpdateToDoItemPriorityAsync(Guid id, Guid userId, TaskPriority priority);
    Task UpdateToDoItemNameAsync(Guid id, Guid userId, string name);
    Task UpdateToDoItemDescriptionAsync(Guid id, Guid userId, string description);
    Task UpdateToDoItemDueDateAsync(Guid id, Guid userId, DateOnly date);
}
