using MailKit.Net.Smtp;

namespace Insequens.Infrastructure.DataAccess.Email;

public sealed class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient Create() => new SmtpClient();
}
