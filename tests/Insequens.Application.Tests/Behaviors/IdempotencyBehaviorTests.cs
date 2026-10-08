using System.Text.Json;
using FluentAssertions;
using Insequens.Application.Abstractions;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V2.Tasks;
using Insequens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application.Tests.Behaviors;

/// <summary>The <c>Idempotency-Key</c> on task creation, through the whole pipeline on SQLite.</summary>
public sealed class IdempotencyBehaviorTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_TwiceWithTheSameKey_CreatesOneTaskAndReturnsTheSameResponse()
    {
        await _database.SeedUserAsync(_userId);

        var first = await _database.SendAsync(Create("key-1"));
        var second = await _database.SendAsync(Create("key-1"));

        second.Should().Be(first);
        (await _database.ItemsAsync(_userId)).Should().ContainSingle().Which.Id.Should().Be(first.Id);
        (await _database.IdempotencyRecordsAsync(_userId)).Should().ContainSingle()
            .Which.ExpiresAt.Should().Be(TestDbContextFactory.StartTime.AddHours(24).UtcDateTime);
    }

    [Fact]
    public async Task Send_WithoutAKey_CreatesATaskEachTime()
    {
        await _database.SeedUserAsync(_userId);

        await _database.SendAsync(Create(null));
        await _database.SendAsync(Create(null));

        (await _database.ItemsAsync(_userId)).Should().HaveCount(2);
        (await _database.IdempotencyRecordsAsync(_userId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Send_TheSameKeyWithADifferentRequest_ThrowsIdempotencyKeyReusedAndCreatesNothing()
    {
        await _database.SeedUserAsync(_userId);
        await _database.SendAsync(Create("key-1"));

        var action = () => _database.SendAsync(Create("key-1", name: "Different"));

        await action.Should().ThrowAsync<IdempotencyKeyReusedException>();
        (await _database.ItemsAsync(_userId)).Should().ContainSingle();
    }

    [Fact]
    public async Task Send_TheSameKeyForAnotherUser_RunsSeparately()
    {
        var otherUser = Guid.NewGuid();
        await _database.SeedUserAsync(_userId);
        await _database.SeedUserAsync(otherUser);

        var mine = await _database.SendAsync(Create("key-1"));
        var theirs = await _database.SendAsync(Create("key-1") with { UserId = otherUser });

        theirs.Id.Should().NotBe(mine.Id);
        (await _database.ItemsAsync(otherUser)).Should().ContainSingle();
    }

    [Fact]
    public async Task Send_AfterTheKeyExpired_RunsAgain()
    {
        await _database.SeedUserAsync(_userId);
        var first = await _database.SendAsync(Create("key-1"));
        _database.Clock.Advance(TimeSpan.FromHours(24));

        var second = await _database.SendAsync(Create("key-1", name: "A new request"));

        second.Id.Should().NotBe(first.Id);
        (await _database.ItemsAsync(_userId)).Should().HaveCount(2);
        (await _database.IdempotencyRecordsAsync(_userId)).Should().ContainSingle()
            .Which.ExpiresAt.Should().Be(TestDbContextFactory.StartTime.AddHours(48).UtcDateTime);
    }

    [Fact]
    public async Task Send_AfterAnInterruptedRequest_IsInProgressOnlyUntilTheLeaseEnds()
    {
        await _database.SeedUserAsync(_userId);
        var interrupted = new FailAfterSaveInterceptor();

        var first = () => _database.SendAsync(Create("key-1"), UseContextWith(interrupted));
        await first.Should().ThrowAsync<InvalidOperationException>();

        var retry = () => _database.SendAsync(Create("key-1"));
        await retry.Should().ThrowAsync<IdempotentRequestInProgressException>();
        (await _database.ItemsAsync(_userId)).Should().ContainSingle("the first request created its task before failing");

        _database.Clock.Advance(IdempotencyRecord.InProgressLease);
        var afterLease = await _database.SendAsync(Create("key-1"));

        (await _database.IdempotencyRecordsAsync(_userId)).Should().ContainSingle().Which.IsCompleted.Should().BeTrue();
        (await _database.SendAsync(Create("key-1"))).Should().Be(afterLease);
    }

    [Fact]
    public async Task Send_WhenAConcurrentRequestRestartsTheExpiredKeyFirst_ReplaysItsResponseAndCreatesNothing()
    {
        await _database.SeedUserAsync(_userId);
        await _database.SendAsync(Create("key-1"));
        _database.Clock.Advance(IdempotencyRecord.Retention);
        var winnerResponse = new TaskResponse(Guid.NewGuid(), "Task", null, TaskPriority.None, null, false);
        var race = new RestartKeyFirstInterceptor(_database, JsonSerializer.Serialize(winnerResponse));

        var response = await _database.SendAsync(Create("key-1"), UseContextWith(race));

        response.Should().Be(winnerResponse);
        (await _database.ItemsAsync(_userId)).Should().ContainSingle("only the first request's task exists");
    }

    [Fact]
    public async Task Send_AfterAFailedRequest_StoresNothingSoTheRetryRuns()
    {
        var failed = () => _database.SendAsync(Create("key-1"));
        await failed.Should().ThrowAsync<DbUpdateException>("the owner does not exist yet");
        (await _database.IdempotencyRecordsAsync(_userId)).Should().BeEmpty();

        await _database.SeedUserAsync(_userId);
        var retried = await _database.SendAsync(Create("key-1"));

        (await _database.ItemsAsync(_userId)).Should().ContainSingle().Which.Id.Should().Be(retried.Id);
    }

    [Fact]
    public async Task Send_WhenAConcurrentRequestClaimsTheKeyFirst_ReplaysItsResponseAndCreatesNothing()
    {
        await _database.SeedUserAsync(_userId);
        var winnerResponse = new TaskResponse(Guid.NewGuid(), "Task", null, TaskPriority.None, null, false);
        var race = new ClaimKeyFirstInterceptor(_database, JsonSerializer.Serialize(winnerResponse));

        var response = await _database.SendAsync(Create("key-1"), UseContextWith(race));

        response.Should().Be(winnerResponse);
        (await _database.ItemsAsync(_userId)).Should().BeEmpty("the losing request's task was in the save that failed");
        (await _database.IdempotencyRecordsAsync(_userId)).Should().ContainSingle();
    }

    private CreateTaskCommand Create(string? key, string name = "Task") =>
        new(_userId, name, null, TaskPriority.None, null, key);

    private Action<IServiceCollection> UseContextWith(IInterceptor interceptor) =>
        services => services.AddScoped<IApplicationDbContext>(_ => _database.CreateContext(interceptor));

    /// <summary>Lets the first save through, then fails, as a crash between the task and its response being stored would.</summary>
    private sealed class FailAfterSaveInterceptor : SaveChangesInterceptor
    {
        private int _saves;

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default) =>
            ++_saves == 1
                ? throw new InvalidOperationException("Simulated crash after the first save.")
                : base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Before the first save, stores a completed record for the same key and request from "another request".</summary>
    private sealed class ClaimKeyFirstInterceptor(TestDbContextFactory database, string responseBody) : SaveChangesInterceptor
    {
        private bool _claimed;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var claim = eventData.Context!.ChangeTracker.Entries<IdempotencyRecord>()
                .SingleOrDefault(entry => entry.State == EntityState.Added)?.Entity;
            if (!_claimed && claim is not null)
            {
                _claimed = true;
                var winner = IdempotencyRecord.Begin(claim.UserId, claim.Key, claim.RequestHash, TestDbContextFactory.StartTime.UtcDateTime);
                winner.Complete(responseBody, TestDbContextFactory.StartTime.UtcDateTime);

                await using var other = database.CreateContext();
                other.IdempotencyRecords.Add(winner);
                await other.SaveChangesAsync(cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>Before the first save, restarts and completes the same expired key from "another request".</summary>
    private sealed class RestartKeyFirstInterceptor(TestDbContextFactory database, string responseBody) : SaveChangesInterceptor
    {
        private bool _restarted;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var restart = eventData.Context!.ChangeTracker.Entries<IdempotencyRecord>()
                .SingleOrDefault(entry => entry.State == EntityState.Modified)?.Entity;
            if (!_restarted && restart is not null)
            {
                _restarted = true;
                var now = database.Clock.GetUtcNow().UtcDateTime.AddSeconds(-1);

                await using var other = database.CreateContext();
                var winner = await other.IdempotencyRecords.SingleAsync(record => record.Id == restart.Id, cancellationToken);
                winner.Restart(restart.RequestHash, now);
                winner.Complete(responseBody, now);
                await other.SaveChangesAsync(cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
