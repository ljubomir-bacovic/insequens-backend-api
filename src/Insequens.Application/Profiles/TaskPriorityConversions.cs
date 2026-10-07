using ContractPriority = Insequens.Contracts.V1.Tasks.TaskPriority;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Profiles;

/// <summary>Converts between the domain and wire priority enums, which share their numeric values.</summary>
internal static class TaskPriorityConversions
{
    public static ContractPriority? ToContract(this DomainPriority? priority) => (ContractPriority?)priority;

    public static DomainPriority ToDomain(this ContractPriority priority) => (DomainPriority)priority;
}
