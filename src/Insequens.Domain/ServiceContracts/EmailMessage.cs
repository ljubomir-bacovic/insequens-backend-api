namespace Insequens.Domain.ServiceContracts;

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string? TextBody);
