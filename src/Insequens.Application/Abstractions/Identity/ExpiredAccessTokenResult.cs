namespace Insequens.Application.Abstractions.Identity;

public sealed record ExpiredAccessTokenResult(bool IsValid, Guid UserId)
{
    public static ExpiredAccessTokenResult Invalid { get; } = new(false, Guid.Empty);

    public static ExpiredAccessTokenResult Valid(Guid userId) => new(true, userId);
}
