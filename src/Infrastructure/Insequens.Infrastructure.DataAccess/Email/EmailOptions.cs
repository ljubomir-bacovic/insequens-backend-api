using System.ComponentModel.DataAnnotations;

namespace Insequens.Infrastructure.DataAccess.Email;

public sealed record EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string SmtpServer { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool UseTls { get; init; } = true;

    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required]
    [EmailAddress]
    public string From { get; init; } = string.Empty;
}
