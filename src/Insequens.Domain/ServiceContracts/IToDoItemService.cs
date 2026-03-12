using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;

namespace Insequens.Domain.ServiceContracts;

public interface IToDoItemService
{
    Task<ToDoItemGetDetailsModel> AddToDoItemAsync(ToDoItemCreateModel toDoItemCreate, Guid userId);
    Task<List<ToDoItemGetListModel>> GetUserToDoItemsAsync(Guid userId, bool isCompleted, int page, int pageSize);
    Task UpdateToDoItemAsync(ToDoItemUpdateModel toDoItemUpdate);
    Task DeleteToDoItemAsync(Guid id);
    Task<ToDoItemGetDetailsModel> GetToDoItem(Guid id);
    Task ToggleToDoItemCompleteAsync(Guid id);
    Task UpdateToDoItemPriorityAsync(Guid id, TaskPriority priority);
    Task UpdateToDoItemNameAsync(Guid id, string name);
    Task UpdateToDoItemDescriptionAsync(Guid id, string description);
    Task UpdateToDoItemDueDateAsync(Guid id, DateOnly date);
}
