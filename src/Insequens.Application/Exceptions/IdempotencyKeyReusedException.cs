namespace Insequens.Application.Exceptions;

/// <summary>An <c>Idempotency-Key</c> was sent again with a different request (422).</summary>
public sealed class IdempotencyKeyReusedException()
    : Exception("This Idempotency-Key was already used with a different request.");
