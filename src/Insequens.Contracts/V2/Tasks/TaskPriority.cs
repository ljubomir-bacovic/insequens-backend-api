using System.Text.Json.Serialization;

namespace Insequens.Contracts.V2.Tasks;

/// <summary>Ascending importance, as in the domain; on the wire <c>"none"</c>, <c>"low"</c>, <c>"medium"</c>, <c>"high"</c>.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<TaskPriority>))]
public enum TaskPriority
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}
