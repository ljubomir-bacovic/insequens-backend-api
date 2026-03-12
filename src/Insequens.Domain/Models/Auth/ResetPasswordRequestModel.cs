namespace Insequens.Domain.Models.Auth;

public class ResetPasswordRequestModel
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}
