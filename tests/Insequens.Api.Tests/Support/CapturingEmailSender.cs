using System.Collections.Concurrent;
using Insequens.Application.Abstractions.Email;

namespace Insequens.Api.Tests.Support;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyCollection<EmailMessage> Messages => _messages.ToArray();

    public Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
