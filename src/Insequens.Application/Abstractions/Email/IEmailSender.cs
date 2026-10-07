namespace Insequens.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken);
}
