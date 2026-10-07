using System.Linq.Expressions;
using V1Priority = Insequens.Contracts.V1.Tasks.TaskPriority;
using DomainPriority = Insequens.Domain.Types.TaskPriority;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Profiles;

/// <summary>
/// The frozen v1 priority contract counts down in importance (High = 1, Medium = 2, Low = 3) and uses null, or 0
/// on create, for none. The domain counts up (None = 0 … High = 3), so a level is <c>4 - value</c> in the other.
/// v2 shares the domain values.
/// </summary>
internal static class TaskPriorityConversions
{
    private const int V1Mirror = 4;

    /// <summary>For read projections, which must stay translatable to SQL.</summary>
    public static readonly Expression<Func<ToDoItemEntity, V1Priority?>> ToV1Projection = item =>
        item.Priority == DomainPriority.None ? null : (V1Priority)(V1Mirror - (int)item.Priority);

    public static V1Priority? ToV1(this DomainPriority priority) =>
        priority == DomainPriority.None ? null : (V1Priority)(V1Mirror - (int)priority);

    public static DomainPriority ToDomain(this V1Priority priority) => (DomainPriority)(V1Mirror - (int)priority);

    /// <summary>The v1 create body's integer: 0 none, 1 high, 2 medium, 3 low.</summary>
    public static DomainPriority FromV1Value(int value) =>
        value == 0 ? DomainPriority.None : (DomainPriority)(V1Mirror - value);
}
