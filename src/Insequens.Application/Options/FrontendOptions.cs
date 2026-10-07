using System.ComponentModel.DataAnnotations;

namespace Insequens.Application.Options;

public sealed record FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>Base URL of the web app. Links in confirmation and password-reset emails point here.</summary>
    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}
