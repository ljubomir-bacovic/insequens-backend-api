using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Insequens.Api.Tests.Support;

/// <summary>Records the text of every SQL command EF sends, to assert how many round trips a request makes.</summary>
public sealed class SqlCommandRecorder : DbCommandInterceptor
{
    private readonly ConcurrentQueue<string> _commands = new();

    public IReadOnlyList<string> Commands => [.. _commands];

    public IReadOnlyList<string> Selects => Commands.Where(command => StartsWith(command, "SELECT")).ToList();

    public void Clear() => _commands.Clear();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        _commands.Enqueue(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        _commands.Enqueue(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        _commands.Enqueue(command.CommandText);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        _commands.Enqueue(command.CommandText);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static bool StartsWith(string command, string keyword) =>
        command.TrimStart().StartsWith(keyword, StringComparison.OrdinalIgnoreCase);
}
