namespace Insequens.Application.Authorization;

/// <summary>A request that acts on one existing resource on behalf of a user.</summary>
public interface IResourceRequest
{
    /// <summary>The caller, taken from the JWT, never from the request body.</summary>
    Guid UserId { get; }

    Guid ResourceId { get; }
}
