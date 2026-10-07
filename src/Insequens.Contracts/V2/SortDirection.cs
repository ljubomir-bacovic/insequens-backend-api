using System.Text.Json.Serialization;

namespace Insequens.Contracts.V2;

[JsonConverter(typeof(CamelCaseStringEnumConverter<SortDirection>))]
public enum SortDirection
{
    Asc,
    Desc,
}
