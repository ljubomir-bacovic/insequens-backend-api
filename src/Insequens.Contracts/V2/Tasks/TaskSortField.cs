using System.Text.Json.Serialization;

namespace Insequens.Contracts.V2.Tasks;

[JsonConverter(typeof(CamelCaseStringEnumConverter<TaskSortField>))]
public enum TaskSortField
{
    DueDate,
    Priority,
    CreatedOn,
    Name,
}
