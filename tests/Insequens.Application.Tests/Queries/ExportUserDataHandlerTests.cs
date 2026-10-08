using FluentAssertions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Exceptions;
using Insequens.Application.Queries.Account;
using Insequens.Application.Tests.Commands.Account;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V1.Account;
using Insequens.Contracts.V1.Tasks;
using NSubstitute;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Tests.Queries;

public sealed class ExportUserDataHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly AccountTestServices _services = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_ReturnsTheAccountWithItsOwnTasksAndSessions()
    {
        _services.IdentityService.GetAccountAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new AccountDetails(_userId, "user@example.com", true, ["Support"]));
        var item = await _database.SeedItemAsync(_userId, "Mine", "Details", DomainPriority.High, new DateOnly(2026, 4, 1));
        var token = await _database.SeedRefreshTokenAsync(_userId, "phone");
        await _database.SeedItemAsync(Guid.NewGuid(), "Someone else's");

        var export = await SendAsync(new ExportUserDataQuery(_userId));

        export.ExportedAt.Should().Be(TestDbContextFactory.StartTime);
        export.Account.Should().BeEquivalentTo(new ExportedAccount(_userId, "user@example.com", true, ["Support"]));
        export.Tasks.Should().ContainSingle().Which.Should().Be(new ExportedTask(
            item.Id, "Mine", "Details", TaskPriority.High, new DateOnly(2026, 4, 1), false,
            TestDbContextFactory.StartTime, TestDbContextFactory.StartTime));
        export.Sessions.Should().ContainSingle().Which.Should().Be(new ExportedSession(
            token.FamilyId, null, null, TestDbContextFactory.StartTime, TestDbContextFactory.StartTime.AddDays(7), null));
    }

    [Fact]
    public async Task Send_ForUnknownAccount_ThrowsNotFound()
    {
        var action = () => SendAsync(new ExportUserDataQuery(_userId));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private Task<UserDataExport> SendAsync(ExportUserDataQuery query) => _database.SendAsync(query, _services.Register);

    [Fact]
    public async Task Send_IncludesTasksInTheTrashWithWhenTheyWereDeleted()
    {
        _services.IdentityService.GetAccountAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new AccountDetails(_userId, "user@example.com", true, []));
        var item = await _database.SeedItemAsync(_userId, "Deleted");
        _database.Clock.Advance(TimeSpan.FromHours(1));
        await _database.SendAsync(new DeleteTaskCommand(item.Id, _userId));

        var export = await SendAsync(new ExportUserDataQuery(_userId));

        export.Tasks.Should().ContainSingle().Which.DeletedOn.Should().Be(TestDbContextFactory.StartTime.AddHours(1));
    }
}
