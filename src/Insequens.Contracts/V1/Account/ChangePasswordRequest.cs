namespace Insequens.Contracts.V1.Account;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
