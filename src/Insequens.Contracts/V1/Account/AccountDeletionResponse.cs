namespace Insequens.Contracts.V1.Account;

/// <param name="DeletesAfter">When the account and its data are permanently deleted. Sign-in stops immediately.</param>
public sealed record AccountDeletionResponse(DateTimeOffset DeletesAfter);
