namespace Journal.Domain.ServiceContracts;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}
