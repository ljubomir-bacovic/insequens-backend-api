namespace Insequens.Contracts.V1.Account;

/// <summary>Everything Insequens stores about one user, for a data-portability request.</summary>
public sealed record UserDataExport(
    DateTimeOffset ExportedAt,
    ExportedAccount Account,
    IReadOnlyList<ExportedTask> Tasks,
    IReadOnlyList<ExportedSession> Sessions);
