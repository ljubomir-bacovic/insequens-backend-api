using System.Text.Json.Serialization;

namespace Insequens.Contracts.V2.Tasks;

/// <summary>The completion state to set; sending it twice leaves the same state (unlike v1's toggle).</summary>
public record SetTaskCompletionRequest([property: JsonRequired] bool Completed);
