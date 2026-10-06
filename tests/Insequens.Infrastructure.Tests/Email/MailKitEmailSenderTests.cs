using FluentAssertions;
using Insequens.Domain.ServiceContracts;
using Insequens.Infrastructure.DataAccess.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;

namespace Insequens.Infrastructure.Tests.Email;

public class MailKitEmailSenderTests
{
    private static readonly EmailMessage Message = new(
        "user@example.com",
        "Subject",
        "<p>Html body</p>",
        "Text body");

    [Fact]
    public async Task SendEmailAsync_WithMessage_SendsMimeMessageBuiltFromMessageAndOptions()
    {
        var client = Substitute.For<ISmtpClient>();
        MimeMessage? sentMessage = null;
        client
            .SendAsync(Arg.Do<MimeMessage>(message => sentMessage = message), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>())
            .Returns(string.Empty);
        var sender = CreateSender(client, CreateOptions(), new FakeLogger<MailKitEmailSender>());

        await sender.SendEmailAsync(Message, CancellationToken.None);

        sentMessage.Should().NotBeNull();
        sentMessage!.From.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("no-reply@localhost");
        sentMessage.To.Mailboxes.Should().ContainSingle().Which.Address.Should().Be(Message.To);
        sentMessage.Subject.Should().Be(Message.Subject);
        sentMessage.HtmlBody.Should().Be(Message.HtmlBody);
        sentMessage.TextBody.Should().Be(Message.TextBody);
    }

    [Fact]
    public async Task SendEmailAsync_WithUsername_ConnectsAuthenticatesSendsAndDisconnectsInOrder()
    {
        var client = Substitute.For<ISmtpClient>();
        var sender = CreateSender(client, CreateOptions(), new FakeLogger<MailKitEmailSender>());
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await sender.SendEmailAsync(Message, cancellationToken);

        NSubstitute.Received.InOrder(() =>
        {
            client.ConnectAsync("localhost", 587, SecureSocketOptions.StartTls, cancellationToken);
            client.AuthenticateAsync("smtp-user", "smtp-password", cancellationToken);
            client.SendAsync(Arg.Any<MimeMessage>(), cancellationToken, Arg.Any<ITransferProgress>());
            client.DisconnectAsync(true, cancellationToken);
        });
        client.Received(1).Dispose();
    }

    [Fact]
    public async Task SendEmailAsync_WithoutUsername_DoesNotAuthenticate()
    {
        var client = Substitute.For<ISmtpClient>();
        var sender = CreateSender(client, CreateOptions() with { Username = string.Empty }, new FakeLogger<MailKitEmailSender>());

        await sender.SendEmailAsync(Message, CancellationToken.None);

        await client.DidNotReceive().AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(587, true, SecureSocketOptions.StartTls)]
    [InlineData(465, true, SecureSocketOptions.SslOnConnect)]
    [InlineData(1025, false, SecureSocketOptions.None)]
    public async Task SendEmailAsync_WithPortAndTlsSetting_ConnectsWithMatchingSocketOptions(
        int port,
        bool useTls,
        SecureSocketOptions expected)
    {
        var client = Substitute.For<ISmtpClient>();
        var sender = CreateSender(client, CreateOptions() with { Port = port, UseTls = useTls }, new FakeLogger<MailKitEmailSender>());

        await sender.SendEmailAsync(Message, CancellationToken.None);

        await client.Received(1).ConnectAsync("localhost", port, expected, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_WhenSent_LogsRecipientDomainButNotAddress()
    {
        var client = Substitute.For<ISmtpClient>();
        var logger = new FakeLogger<MailKitEmailSender>();
        var sender = CreateSender(client, CreateOptions(), logger);

        await sender.SendEmailAsync(Message, CancellationToken.None);

        logger.Collector.LatestRecord.Message.Should().Contain("example.com");
        logger.Collector.GetSnapshot().Should().NotContain(record => record.Message.Contains(Message.To));
    }

    [Fact]
    public async Task SendEmailAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        var client = Substitute.For<ISmtpClient>();
        var sender = CreateSender(client, CreateOptions(), new FakeLogger<MailKitEmailSender>());

        var action = () => sender.SendEmailAsync(null!, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentNullException>();
        await client.DidNotReceive().SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>());
    }

    private static EmailOptions CreateOptions() => new()
    {
        SmtpServer = "localhost",
        Port = 587,
        UseTls = true,
        Username = "smtp-user",
        Password = "smtp-password",
        From = "no-reply@localhost",
    };

    private static MailKitEmailSender CreateSender(
        ISmtpClient client,
        EmailOptions options,
        FakeLogger<MailKitEmailSender> logger)
    {
        var factory = Substitute.For<ISmtpClientFactory>();
        factory.Create().Returns(client);

        return new MailKitEmailSender(Options.Create(options), factory, logger);
    }
}
