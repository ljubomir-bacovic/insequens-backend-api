namespace Insequens.Contracts.V1.Tasks;

/// <summary>Task priority on the wire. Values match the domain enum so v1 JSON is unchanged.</summary>
public enum TaskPriority
{
    Low = 3,
    Medium = 2,
    High = 1,
}
