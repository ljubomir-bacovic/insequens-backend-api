namespace Insequens.Application.Abstractions.Identity;

public sealed record AccountDetails(Guid Id, string Email, bool EmailConfirmed, IReadOnlyList<string> Roles);
