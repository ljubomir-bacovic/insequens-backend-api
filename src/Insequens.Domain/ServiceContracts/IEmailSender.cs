namespace Insequens.Domain.ServiceContracts;

public interface IEmailSender
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken);
}
