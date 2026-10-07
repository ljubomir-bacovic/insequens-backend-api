using System.Text.Json;
using System.Text.Json.Serialization;

namespace Insequens.Contracts.V2;

/// <summary>v2 enums travel as camelCase names (<c>"high"</c>); numbers are rejected.</summary>
public sealed class CamelCaseStringEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    where TEnum : struct, Enum;
