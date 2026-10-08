using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;

namespace Insequens.Api.Tests.V2;

/// <summary>
/// The v2 task contract over HTTP: string priorities, list filters, the partial PATCH, the completion PUT, the trash
/// and idempotency keys.
/// </summary>
public sealed class TasksEndpointTests : IAsyncLifetime
{
    private readonly InsequensApiFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateHttpsClient();
        _client.UseBearer(_factory.CreateAccessToken(Guid.NewGuid()));

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Create_WithAStringPriority_ReadsItBack()
    {
        var created = await CreateAsync("""{ "name": "Task", "priority": "high" }""");

        (await GetAsync(created)).GetProperty("priority").GetString().Should().Be("high");
    }

    [Fact]
    public async Task Create_WithoutAPriority_ReadsBackNone()
    {
        var created = await CreateAsync("""{ "name": "Task" }""");

        (await GetAsync(created)).GetProperty("priority").GetString().Should().Be("none");
    }

    [Theory]
    [InlineData("""{ "name": "Task", "priority": 3 }""")]
    [InlineData("""{ "name": "Task", "priority": "urgent" }""")]
    public async Task Create_WithANumericOrUnknownPriority_Returns400(string body)
    {
        var response = await _client.PostAsync("/v2/Tasks", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_SortedByPriority_PlacesHighFirst()
    {
        await CreateAsync("""{ "name": "Low", "priority": "low" }""");
        await CreateAsync("""{ "name": "High", "priority": "high" }""");
        await CreateAsync("""{ "name": "None" }""");

        var names = await NamesAsync("/v2/Tasks?sortBy=priority");

        names.Should().Equal("High", "Low", "None");
    }

    [Fact]
    public async Task List_WithFilters_ReturnsOnlyTheMatchingTasks()
    {
        await CreateAsync("""{ "name": "Pay rent", "priority": "high", "dueDate": "2026-10-10" }""");
        await CreateAsync("""{ "name": "Pay gas", "priority": "low", "dueDate": "2026-10-12" }""");
        await CreateAsync("""{ "name": "Walk", "priority": "high" }""");

        (await NamesAsync("/v2/Tasks?search=pay&priority=high")).Should().Equal("Pay rent");
        (await NamesAsync("/v2/Tasks?dueFrom=2026-10-11&dueTo=2026-10-31")).Should().Equal("Pay gas");
        (await NamesAsync("/v2/Tasks?completed=false&sortBy=name&sortDirection=desc")).Should().Equal("Walk", "Pay rent", "Pay gas");
    }

    [Fact]
    public async Task List_PagingThrough25TasksBy10_SeesEachTaskExactlyOnce()
    {
        var created = new List<Guid>();
        for (var index = 0; index < 25; index++)
        {
            created.Add(await CreateAsync("""{ "name": "Same", "dueDate": "2026-10-10" }"""));
        }

        var seen = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            using var json = JsonDocument.Parse(await _client.GetStringAsync($"/v2/Tasks?page={page}&pageSize=10"));
            seen.AddRange(json.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
        }

        seen.Should().OnlyHaveUniqueItems().And.BeEquivalentTo(created);
    }

    [Theory]
    [InlineData("sortBy=colour")]
    [InlineData("sortBy=1")]
    [InlineData("priority=3")]
    [InlineData("priority=-1")]
    [InlineData("sortDirection=0")]
    public async Task List_WithANumericOrUnknownEnumValue_Returns400(string query)
    {
        var response = await _client.GetAsync($"/v2/Tasks?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_WithEnumNamesInAnyCase_Binds()
    {
        await CreateAsync("""{ "name": "High", "priority": "high" }""");
        await CreateAsync("""{ "name": "Low", "priority": "low" }""");

        (await NamesAsync("/v2/Tasks?sortBy=PRIORITY&sortDirection=Asc&priority=High")).Should().Equal("High");
    }

    [Fact]
    public async Task PutCompletion_Twice_YieldsTheSameState()
    {
        var id = await CreateAsync("""{ "name": "Task" }""");

        var first = await _client.PutAsync($"/v2/Tasks/{id}/completion", Json("""{ "completed": true }"""));
        var second = await _client.PutAsync($"/v2/Tasks/{id}/completion", Json("""{ "completed": true }"""));

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetAsync(id)).GetProperty("isCompleted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PutCompletion_WithoutTheField_Returns400()
    {
        var id = await CreateAsync("""{ "name": "Task" }""");

        var response = await _client.PutAsync($"/v2/Tasks/{id}/completion", Json("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_WithANullDueDate_ClearsIt()
    {
        var id = await CreateAsync("""{ "name": "Task", "dueDate": "2026-10-10", "description": "Keep me" }""");

        var response = await PatchAsync(id, """{ "dueDate": null }""");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var task = await GetAsync(id);
        task.GetProperty("dueDate").ValueKind.Should().Be(JsonValueKind.Null);
        task.GetProperty("description").GetString().Should().Be("Keep me");
    }

    [Fact]
    public async Task Patch_WithAnEmptyBody_ChangesNothing()
    {
        var id = await CreateAsync("""{ "name": "Task", "description": "Details", "priority": "medium", "dueDate": "2026-10-10" }""");
        var before = (await GetAsync(id)).GetRawText();

        var response = await PatchAsync(id, "{}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetAsync(id)).GetRawText().Should().Be(before);
    }

    [Fact]
    public async Task Patch_WithAnEmptyName_Returns400()
    {
        var id = await CreateAsync("""{ "name": "Task" }""");

        var response = await PatchAsync(id, """{ "name": "" }""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("Name", out _).Should().BeTrue();
        (await GetAsync(id)).GetProperty("name").GetString().Should().Be("Task");
    }

    [Fact]
    public async Task Patch_WithSeveralFields_ChangesOnlyThose()
    {
        var id = await CreateAsync("""{ "name": "Task", "description": "Details", "dueDate": "2026-10-10" }""");

        await PatchAsync(id, """{ "name": "Renamed", "priority": "high" }""");

        var task = await GetAsync(id);
        task.GetProperty("name").GetString().Should().Be("Renamed");
        task.GetProperty("priority").GetString().Should().Be("high");
        task.GetProperty("description").GetString().Should().Be("Details");
        task.GetProperty("dueDate").GetString().Should().Be("2026-10-10");
    }

    [Fact]
    public async Task Delete_MovesTheTaskToTheTrashAndRestoreBringsItBack()
    {
        var id = await CreateAsync("""{ "name": "Task" }""");
        await CreateAsync("""{ "name": "Kept" }""");

        (await _client.DeleteAsync($"/v2/Tasks/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.GetAsync($"/v2/Tasks/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await NamesAsync("/v2/Tasks")).Should().Equal("Kept");
        (await NamesAsync("/v2/Tasks?deleted=true")).Should().Equal("Task");

        (await _client.PostAsync($"/v2/Tasks/{id}/restore", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetAsync(id)).GetProperty("name").GetString().Should().Be("Task");
        (await NamesAsync("/v2/Tasks?deleted=true")).Should().BeEmpty();
    }

    [Fact]
    public async Task Restore_AnUnknownTask_Returns404()
    {
        var response = await _client.PostAsync($"/v2/Tasks/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_TwiceWithTheSameIdempotencyKey_CreatesOneTaskAndReturnsIdenticalBodies()
    {
        const string body = """{ "name": "Offline task", "priority": "high", "dueDate": "2026-10-10" }""";

        var first = await PostWithKeyAsync("key-1", body);
        var second = await PostWithKeyAsync("key-1", body);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        (await second.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());
        second.Headers.Location.Should().Be(first.Headers.Location);
        (await NamesAsync("/v2/Tasks")).Should().Equal("Offline task");
    }

    [Fact]
    public async Task Create_WithAReusedIdempotencyKeyAndADifferentBody_Returns422()
    {
        await PostWithKeyAsync("key-1", """{ "name": "First" }""");

        var response = await PostWithKeyAsync("key-1", """{ "name": "Second" }""");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("type").GetString().Should().Be("urn:insequens:error:idempotency-key-reused");
        (await NamesAsync("/v2/Tasks")).Should().Equal("First");
    }

    [Fact]
    public async Task Create_WithATooLongIdempotencyKey_Returns400()
    {
        var response = await PostWithKeyAsync(new string('k', 101), """{ "name": "Task" }""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V1_ReadsTheSameTaskInItsOwnContract()
    {
        var id = await CreateAsync("""{ "name": "Task", "priority": "high" }""");
        var none = await CreateAsync("""{ "name": "No priority" }""");

        using var v1 = JsonDocument.Parse(await _client.GetStringAsync($"/v1/ToDoItem/{id}"));
        using var v1None = JsonDocument.Parse(await _client.GetStringAsync($"/v1/ToDoItem/{none}"));

        v1.RootElement.GetProperty("priority").GetInt32().Should().Be(1, "v1 numbers high as 1");
        v1None.RootElement.GetProperty("priority").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task OpenApi_MarksTheV1ToggleDeprecated()
    {
        using var json = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json"));

        json.RootElement.GetProperty("paths").GetProperty("/v1/ToDoItem/{id}/togglecomplete").GetProperty("patch")
            .GetProperty("deprecated").GetBoolean().Should().BeTrue();
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private async Task<Guid> CreateAsync(string body)
    {
        var response = await _client.PostAsync("/v2/Tasks", Json(body));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        response.Headers.Location!.AbsolutePath.Should().StartWith("/v2/Tasks/");

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> GetAsync(Guid id) =>
        await _client.GetFromJsonAsync<JsonElement>($"/v2/Tasks/{id}");

    private async Task<List<string>> NamesAsync(string url)
    {
        using var json = JsonDocument.Parse(await _client.GetStringAsync(url));

        return [.. json.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()!)];
    }

    private Task<HttpResponseMessage> PatchAsync(Guid id, string body) => _client.PatchAsync($"/v2/Tasks/{id}", Json(body));

    private Task<HttpResponseMessage> PostWithKeyAsync(string key, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/Tasks") { Content = Json(body) };
        request.Headers.Add("Idempotency-Key", key);

        return _client.SendAsync(request);
    }
}
