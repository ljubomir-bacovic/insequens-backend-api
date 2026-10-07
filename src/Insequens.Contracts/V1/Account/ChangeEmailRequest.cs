namespace Insequens.Contracts.V1.Account;

public sealed record ChangeEmailRequest(string NewEmail, string CurrentPassword);
