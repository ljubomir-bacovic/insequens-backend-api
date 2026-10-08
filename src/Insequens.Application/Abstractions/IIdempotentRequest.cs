namespace Insequens.Application.Abstractions;

/// <summary>
/// A request a client may retry safely by sending the same <see cref="IdempotencyKey"/>: the
/// <see cref="Behaviors.IdempotencyBehavior{TRequest,TResponse}"/> runs it once and replays its response.
/// </summary>
public interface IIdempotentRequest
{
    Guid UserId { get; }

    /// <summary>The client's <c>Idempotency-Key</c>; null runs the request without that protection.</summary>
    string? IdempotencyKey { get; }
}
