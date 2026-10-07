namespace Insequens.Contracts.V1.Account;

/// <summary>The values from the link in the confirmation email sent to the new address.</summary>
public sealed record ConfirmEmailChangeRequest(string UserId, string NewEmail, string Token);
