namespace Insequens.Contracts.V1.Auth;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
