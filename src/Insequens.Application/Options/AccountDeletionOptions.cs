using System.ComponentModel.DataAnnotations;

namespace Insequens.Application.Options;

public sealed record AccountDeletionOptions
{
    public const string SectionName = "AccountDeletion";

    /// <summary>How long a deleted account stays recoverable by support before its data is purged.</summary>
    [Range(typeof(TimeSpan), "00:00:00", "365.00:00:00", ErrorMessage = "AccountDeletion:GracePeriod must be between 0 and 365 days.")]
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromDays(30);
}
