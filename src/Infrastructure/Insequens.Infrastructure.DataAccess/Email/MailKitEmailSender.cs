using Insequens.Domain.ServiceContracts;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Insequens.Infrastructure.DataAccess.Email;

public sealed class MailKitEmailSender(
    IOptions<EmailOptions> options,
    ISmtpClientFactory smtpClientFactory,
    ILogger<MailKitEmailSender> logger) : IEmailSender
{
    private const int ImplicitTlsPort = 465;

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var emailOptions = options.Value;
        var mimeMessage = CreateMimeMessage(message, emailOptions.From);

        using var client = smtpClientFactory.Create();
        await client.ConnectAsync(
            emailOptions.SmtpServer,
            emailOptions.Port,
            GetSecureSocketOptions(emailOptions),
            cancellationToken);

        if (!string.IsNullOrEmpty(emailOptions.Username))
        {
            await client.AuthenticateAsync(emailOptions.Username, emailOptions.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Sent email to recipient domain {RecipientDomain}", GetDomain(message.To));
    }

    private static MimeMessage CreateMimeMessage(EmailMessage message, string from)
    {
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        };

        var mimeMessage = new MimeMessage
        {
            Subject = message.Subject,
            Body = bodyBuilder.ToMessageBody(),
        };
        mimeMessage.From.Add(MailboxAddress.Parse(from));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        return mimeMessage;
    }

    private static SecureSocketOptions GetSecureSocketOptions(EmailOptions emailOptions)
    {
        if (!emailOptions.UseTls)
        {
            return SecureSocketOptions.None;
        }

        return emailOptions.Port == ImplicitTlsPort
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static string GetDomain(string address)
    {
        var separatorIndex = address.LastIndexOf('@');

        return separatorIndex >= 0 ? address[(separatorIndex + 1)..] : string.Empty;
    }
}
