using MailKit.Net.Smtp;

namespace Insequens.Infrastructure.DataAccess.Email;

public interface ISmtpClientFactory
{
    ISmtpClient Create();
}
