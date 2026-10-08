using System.Security.Cryptography;
using System.Text.Json;
using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using Insequens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Behaviors;

/// <summary>
/// Runs an <see cref="IIdempotentRequest"/> with a key at most once per user and key, and replays its stored response
/// to every retry within <see cref="IdempotencyRecord.Retention"/>. The key is claimed in the same save as the
/// handler's changes, so of two concurrent requests with one key only one commits; the other replays its response.
/// A failed request stores nothing, so the client can retry it with the same key. The type constraint means the
/// container builds this behavior only for idempotent requests.
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse>(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentRequest
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is not { } key)
        {
            return await next(cancellationToken);
        }

        var requestHash = Hash(request);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var record = await FindAsync(request.UserId, key, cancellationToken);
        if (record is not null && !record.IsExpired(now))
        {
            return Replay(record, requestHash);
        }

        if (record is null)
        {
            record = IdempotencyRecord.Begin(request.UserId, key, requestHash, now);
            dbContext.IdempotencyRecords.Add(record);
        }
        else
        {
            record.Restart(requestHash, now);
        }

        TResponse response;
        try
        {
            response = await next(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request claimed the key between our read and our save; nothing of ours was saved.
            var winner = await dbContext.IdempotencyRecords.AsNoTracking()
                .SingleOrDefaultAsync(other => other.UserId == request.UserId && other.Key == key, cancellationToken);
            if (winner is null || winner.Id == record.Id)
            {
                throw;
            }

            return Replay(winner, requestHash);
        }

        record.Complete(JsonSerializer.Serialize(response));
        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    private Task<IdempotencyRecord?> FindAsync(Guid userId, string key, CancellationToken cancellationToken) =>
        dbContext.IdempotencyRecords.SingleOrDefaultAsync(
            record => record.UserId == userId && record.Key == key,
            cancellationToken);

    private static TResponse Replay(IdempotencyRecord record, string requestHash)
    {
        if (!record.Matches(requestHash))
        {
            throw new IdempotencyKeyReusedException();
        }

        if (record.ResponseBody is not { } responseBody)
        {
            throw new IdempotentRequestInProgressException();
        }

        return JsonSerializer.Deserialize<TResponse>(responseBody)!;
    }

    /// <summary>The request's JSON, which includes the user and the key, so equal hashes mean the same request.</summary>
    private static string Hash(TRequest request) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request)));
}
