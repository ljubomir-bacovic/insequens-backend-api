using MailKit.Net.Smtp;

namespace Insequens.Infrastructure.Email;

public sealed class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient Create() => new SmtpClient();
}
