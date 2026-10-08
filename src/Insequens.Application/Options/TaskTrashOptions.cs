using System.ComponentModel.DataAnnotations;

namespace Insequens.Application.Options;

public sealed record TaskTrashOptions
{
    public const string SectionName = "TaskTrash";

    /// <summary>How long a deleted task stays in the trash, restorable, before it is purged.</summary>
    [Range(typeof(TimeSpan), "1.00:00:00", "365.00:00:00", ErrorMessage = "TaskTrash:Retention must be between 1 and 365 days.")]
    public TimeSpan Retention { get; init; } = TimeSpan.FromDays(30);
}
