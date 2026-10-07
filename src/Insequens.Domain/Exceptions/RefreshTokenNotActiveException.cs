namespace Insequens.Domain.Exceptions;

public sealed class RefreshTokenNotActiveException()
    : DomainException("Only an active refresh token can be rotated.");
