namespace Insequens.Domain.Models.Auth;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
