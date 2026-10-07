using System.Text.Json.Serialization;

namespace Insequens.Contracts.V2;

/// <summary>
/// A request field that can be absent, which is not the same as present and null: in a PATCH, an absent
/// <c>dueDate</c> leaves the date alone and <c>"dueDate": null</c> clears it. <c>default</c> is absent.
/// </summary>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    public Optional(T value)
    {
        Value = value;
        HasValue = true;
    }

    public bool HasValue { get; }

    public T Value { get; }

    public static implicit operator Optional<T>(T value) => new(value);
}
