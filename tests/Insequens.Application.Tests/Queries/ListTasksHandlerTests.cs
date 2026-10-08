using FluentAssertions;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Queries.Tasks;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Tests.Queries;

public sealed class ListTasksHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithoutFilters_ReturnsOnlyTheCallersTasksWithStringPriorities()
    {
        await _database.SeedItemAsync(_userId, "Mine", priority: DomainPriority.High);
        await _database.SeedItemAsync(Guid.NewGuid(), "Someone else's");

        var result = await ListAsync();

        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Mine", Priority = TaskPriority.High });
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Send_HidesDeletedTasksUnlessAskedForTheTrash()
    {
        await _database.SeedItemAsync(_userId, "Live");
        var deleted = await _database.SeedItemAsync(_userId, "Deleted");
        var foreign = await _database.SeedItemAsync(Guid.NewGuid(), "Someone else's deleted");
        await _database.SendAsync(new DeleteTaskCommand(deleted.Id, _userId));
        await _database.SendAsync(new DeleteTaskCommand(foreign.Id, foreign.UserId));

        var live = await ListAsync();
        var trash = await ListAsync(new ListTasksQuery(_userId, Deleted: true));

        live.Items.Select(item => item.Name).Should().Equal("Live");
        trash.Items.Select(item => item.Name).Should().Equal("Deleted");
        trash.TotalCount.Should().Be(1);
    }

    [Theory]
    [InlineData(true, "Done")]
    [InlineData(false, "Open")]
    public async Task Send_WithCompleted_ReturnsOnlyTasksInThatState(bool completed, string expected)
    {
        await _database.SeedItemAsync(_userId, "Done", isCompleted: true);
        await _database.SeedItemAsync(_userId, "Open");

        var result = await ListAsync(new ListTasksQuery(_userId, Completed: completed));

        result.Items.Select(item => item.Name).Should().Equal(expected);
    }

    [Fact]
    public async Task Send_WithPriority_ReturnsOnlyTasksWithThatPriority()
    {
        await _database.SeedItemAsync(_userId, "High", priority: DomainPriority.High);
        await _database.SeedItemAsync(_userId, "None");

        var result = await ListAsync(new ListTasksQuery(_userId, Priority: TaskPriority.None));

        result.Items.Select(item => item.Name).Should().Equal("None");
    }

    [Fact]
    public async Task Send_WithADueDateRange_ReturnsTasksDueWithinItInclusive()
    {
        await _database.SeedItemAsync(_userId, "Before", dueDate: new DateOnly(2026, 3, 9));
        await _database.SeedItemAsync(_userId, "First day", dueDate: new DateOnly(2026, 3, 10));
        await _database.SeedItemAsync(_userId, "Last day", dueDate: new DateOnly(2026, 3, 20));
        await _database.SeedItemAsync(_userId, "After", dueDate: new DateOnly(2026, 3, 21));
        await _database.SeedItemAsync(_userId, "No due date");

        var result = await ListAsync(new ListTasksQuery(_userId, DueFrom: new DateOnly(2026, 3, 10), DueTo: new DateOnly(2026, 3, 20)));

        result.Items.Select(item => item.Name).Should().Equal("First day", "Last day");
    }

    [Fact]
    public async Task Send_WithSearch_MatchesNameOrDescriptionIgnoringCase()
    {
        await _database.SeedItemAsync(_userId, "Buy MILK");
        await _database.SeedItemAsync(_userId, "Groceries", description: "milk and bread");
        await _database.SeedItemAsync(_userId, "Call the bank");

        var result = await ListAsync(new ListTasksQuery(_userId, Search: "  milk "));

        result.Items.Select(item => item.Name).Should().BeEquivalentTo("Buy MILK", "Groceries");
    }

    [Theory]
    [InlineData("100%", "Discount 100%")]
    [InlineData("a_b", "File a_b")]
    public async Task Send_WithSearchContainingWildcards_MatchesThemLiterally(string search, string expected)
    {
        await _database.SeedItemAsync(_userId, "Discount 100%");
        await _database.SeedItemAsync(_userId, "Discount 1000");
        await _database.SeedItemAsync(_userId, "File a_b");
        await _database.SeedItemAsync(_userId, "File axb");

        var result = await ListAsync(new ListTasksQuery(_userId, Search: search));

        result.Items.Select(item => item.Name).Should().Equal(expected);
    }

    [Fact]
    public async Task Send_SortedByDueDate_PutsTasksWithoutOneLastAndBreaksTiesByPriority()
    {
        await _database.SeedItemAsync(_userId, "No due date", priority: DomainPriority.High);
        await _database.SeedItemAsync(_userId, "Later", dueDate: new DateOnly(2026, 4, 2));
        await _database.SeedItemAsync(_userId, "Sooner, low", priority: DomainPriority.Low, dueDate: new DateOnly(2026, 4, 1));
        await _database.SeedItemAsync(_userId, "Sooner, high", priority: DomainPriority.High, dueDate: new DateOnly(2026, 4, 1));

        var ascending = await ListAsync(new ListTasksQuery(_userId));
        var descending = await ListAsync(new ListTasksQuery(_userId, SortDirection: SortDirection.Desc));

        ascending.Items.Select(item => item.Name).Should().Equal("Sooner, high", "Sooner, low", "Later", "No due date");
        descending.Items.Select(item => item.Name).Should().Equal("Later", "Sooner, high", "Sooner, low", "No due date");
    }

    [Fact]
    public async Task Send_SortedByPriority_PutsHighFirstUnlessAscendingIsAsked()
    {
        await _database.SeedItemAsync(_userId, "Medium", priority: DomainPriority.Medium);
        await _database.SeedItemAsync(_userId, "None");
        await _database.SeedItemAsync(_userId, "High", priority: DomainPriority.High);
        await _database.SeedItemAsync(_userId, "Low", priority: DomainPriority.Low);

        var byDefault = await ListAsync(new ListTasksQuery(_userId, SortBy: TaskSortField.Priority));
        var ascending = await ListAsync(new ListTasksQuery(_userId, SortBy: TaskSortField.Priority, SortDirection: SortDirection.Asc));

        byDefault.Items.Select(item => item.Name).Should().Equal("High", "Medium", "Low", "None");
        ascending.Items.Select(item => item.Name).Should().Equal("None", "Low", "Medium", "High");
    }

    [Fact]
    public async Task Send_SortedByCreatedOn_PutsTheNewestFirst()
    {
        await _database.SeedItemAsync(_userId, "Oldest");
        _database.Clock.Advance(TimeSpan.FromMinutes(1));
        await _database.SeedItemAsync(_userId, "Newest");

        var result = await ListAsync(new ListTasksQuery(_userId, SortBy: TaskSortField.CreatedOn));

        result.Items.Select(item => item.Name).Should().Equal("Newest", "Oldest");
    }

    [Fact]
    public async Task Send_SortedByName_OrdersAlphabetically()
    {
        await _database.SeedItemAsync(_userId, "Banana");
        await _database.SeedItemAsync(_userId, "Apple");

        var ascending = await ListAsync(new ListTasksQuery(_userId, SortBy: TaskSortField.Name));
        var descending = await ListAsync(new ListTasksQuery(_userId, SortBy: TaskSortField.Name, SortDirection: SortDirection.Desc));

        ascending.Items.Select(item => item.Name).Should().Equal("Apple", "Banana");
        descending.Items.Select(item => item.Name).Should().Equal("Banana", "Apple");
    }

    [Theory]
    [InlineData(TaskSortField.DueDate)]
    [InlineData(TaskSortField.Priority)]
    [InlineData(TaskSortField.CreatedOn)]
    [InlineData(TaskSortField.Name)]
    public async Task Send_PagingThroughTiedTasks_ReturnsEachTaskExactlyOnce(TaskSortField sortBy)
    {
        var seeded = new List<Guid>();
        for (var index = 0; index < 25; index++)
        {
            seeded.Add((await _database.SeedItemAsync(_userId, "Same name", dueDate: new DateOnly(2026, 5, 1))).Id);
        }

        var seen = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await ListAsync(new ListTasksQuery(_userId, SortBy: sortBy, Page: page, PageSize: 10));
            seen.AddRange(result.Items.Select(item => item.Id));
        }

        seen.Should().OnlyHaveUniqueItems().And.BeEquivalentTo(seeded);
    }

    private Task<Contracts.V1.PaginatedResult<TaskResponse>> ListAsync(ListTasksQuery? query = null) =>
        _database.SendAsync(query ?? new ListTasksQuery(_userId));
}
