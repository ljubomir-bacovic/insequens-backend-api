namespace Insequens.Contracts.V1.Account;

public sealed record ExportedAccount(Guid Id, string Email, bool EmailConfirmed, IReadOnlyList<string> Roles);
