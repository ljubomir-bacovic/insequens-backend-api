using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Contracts.V1.Tasks;

namespace Insequens.Api.Tests.ToDoItems;

/// <summary>ETag and If-Match on SQL Server, where the row version is real.</summary>
public sealed class OptimisticConcurrencyTests(SqlServerContainerFixture sqlServer)
    : IClassFixture<SqlServerContainerFixture>, IAsyncLifetime
{
    private InsequensApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new InsequensApiFactory(sqlServerConnectionString: sqlServer.NewDatabase());
        var user = await _factory.CreateUserAsync(AuthTestHelpers.Email, AuthTestHelpers.Password);
        _client = _factory.CreateHttpsClient();
        _client.UseBearer(_factory.CreateAccessToken(user.Id));
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Get_ReturnsTheRowVersionAsAStrongETag()
    {
        var id = await CreateTaskAsync();

        var response = await _client.GetAsync($"/v1/ToDoItem/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.IsWeak.Should().BeFalse();
        response.Headers.ETag.Tag.Should().MatchRegex("^\"[A-Za-z0-9+/]{11}=\"$", "a rowversion is eight bytes");
    }

    [Fact]
    public async Task Patch_WithTheCurrentETag_Returns204AndTheETagChanges()
    {
        var id = await CreateTaskAsync();
        var etag = await ETagAsync(id);

        var response = await PatchNameAsync(id, "Renamed", etag);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ETagAsync(id)).Should().NotBe(etag);
    }

    [Fact]
    public async Task Patch_WithAStaleETag_Returns412AndKeepsTheTask()
    {
        var id = await CreateTaskAsync();
        var stale = await ETagAsync(id);
        (await PatchNameAsync(id, "First change", stale)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await PatchNameAsync(id, "Second change", stale);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("type").GetString().Should().Be("urn:insequens:error:precondition-failed");
        (await NameAsync(id)).Should().Be("First change");
    }

    [Fact]
    public async Task ConcurrentPatches_WithTheSameIfMatch_OneSucceedsAndTheOtherGets412()
    {
        var id = await CreateTaskAsync();
        var etag = await ETagAsync(id);

        var responses = await Task.WhenAll(PatchNameAsync(id, "Alpha", etag), PatchNameAsync(id, "Beta", etag));

        responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
            [HttpStatusCode.NoContent, HttpStatusCode.PreconditionFailed]);
        var winner = responses[0].StatusCode == HttpStatusCode.NoContent ? "Alpha" : "Beta";
        (await NameAsync(id)).Should().Be(winner);
    }

    [Fact]
    public async Task Delete_WithAStaleETag_Returns412AndKeepsTheTask()
    {
        var id = await CreateTaskAsync();
        var stale = await ETagAsync(id);
        (await PatchNameAsync(id, "Changed", stale)).EnsureSuccessStatusCode();

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/v1/ToDoItem/{id}");
        request.Headers.IfMatch.Add(stale);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        (await _client.GetAsync($"/v1/ToDoItem/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Patch_WithoutIfMatchOrWithAStar_SkipsTheCheck()
    {
        var id = await CreateTaskAsync();

        (await PatchNameAsync(id, "No precondition", ifMatch: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await PatchNameAsync(id, "Any version", EntityTagHeaderValue.Any)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await NameAsync(id)).Should().Be("Any version");
    }

    [Theory]
    [InlineData("W/\"AAAAAAAAB9E=\"")]
    [InlineData("\"not base64!\"")]
    [InlineData("\"AAAAAAAAB9E=\", \"AAAAAAAAB9I=\"")]
    public async Task Patch_WithAnIfMatchThatCannotMatch_Returns412ProblemDetails(string ifMatch)
    {
        var id = await CreateTaskAsync();
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/v1/ToDoItem/{id}/name")
        {
            Content = JsonContent.Create("Renamed"),
        };
        request.Headers.TryAddWithoutValidation("If-Match", ifMatch);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await NameAsync(id)).Should().Be("Task");
    }

    private async Task<Guid> CreateTaskAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/ToDoItem", new ToDoItemCreateModel("Task", null, 1, null));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ToDoItemGetDetailsModel>())!.Id;
    }

    private async Task<EntityTagHeaderValue> ETagAsync(Guid id) =>
        (await _client.GetAsync($"/v1/ToDoItem/{id}")).Headers.ETag!;

    private async Task<string> NameAsync(Guid id) =>
        (await _client.GetFromJsonAsync<ToDoItemGetDetailsModel>($"/v1/ToDoItem/{id}"))!.Name;

    private async Task<HttpResponseMessage> PatchNameAsync(Guid id, string name, EntityTagHeaderValue? ifMatch)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/v1/ToDoItem/{id}/name")
        {
            Content = JsonContent.Create(name),
        };
        if (ifMatch is not null)
        {
            request.Headers.IfMatch.Add(ifMatch);
        }

        return await _client.SendAsync(request);
    }
}
