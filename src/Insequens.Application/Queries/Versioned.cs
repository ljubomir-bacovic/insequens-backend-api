namespace Insequens.Application.Queries;

/// <summary>A read model with the version of the entity it was read from, which the API sends as the ETag.</summary>
public sealed record Versioned<T>(T Value, byte[] Version);
