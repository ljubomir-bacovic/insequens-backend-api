namespace Insequens.Application.Exceptions;

/// <summary>The first request with this <c>Idempotency-Key</c> has not finished yet (409).</summary>
public sealed class IdempotentRequestInProgressException()
    : Exception("A request with this Idempotency-Key is still being processed.");
