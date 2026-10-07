using MailKit.Net.Smtp;

namespace Insequens.Infrastructure.Email;

public interface ISmtpClientFactory
{
    ISmtpClient Create();
}
