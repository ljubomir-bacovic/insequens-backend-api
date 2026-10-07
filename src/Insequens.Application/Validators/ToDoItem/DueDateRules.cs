using FluentValidation;

namespace Insequens.Application.Validators.ToDoItem;

/// <summary>A due date, when present, lies within ten years of today (UTC).</summary>
public static class DueDateRules
{
    public const int MaximumYearsFromToday = 10;

    public static IRuleBuilderOptions<T, DateOnly?> WithinDueDateRange<T>(
        this IRuleBuilder<T, DateOnly?> ruleBuilder,
        TimeProvider timeProvider) =>
        ruleBuilder
            .Must(dueDate => dueDate is null || IsWithinRange(dueDate.Value, timeProvider))
            .WithMessage($"Due date must be within {MaximumYearsFromToday} years of today.");

    private static bool IsWithinRange(DateOnly dueDate, TimeProvider timeProvider)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return dueDate >= today.AddYears(-MaximumYearsFromToday) && dueDate <= today.AddYears(MaximumYearsFromToday);
    }
}
