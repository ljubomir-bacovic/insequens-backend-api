using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Application.Commands.Tasks;
using Insequens.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Api.Tests.V2;

/// <summary>The trash and idempotency keys on SQL Server, where row versions, unique indexes and set-based deletes are real.</summary>
public sealed class TasksSqlServerTests(SqlServerContainerFixture sqlServer)
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
    public async Task ConcurrentPosts_WithTheSameIdempotencyKey_CreateOneTask()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => PostWithKeyAsync("key-1")));

        responses.Select(response => response.StatusCode).Should()
            .OnlyContain(status => status == HttpStatusCode.Created || status == HttpStatusCode.Conflict)
            .And.Contain(HttpStatusCode.Created);
        var bodies = await Task.WhenAll(responses
            .Where(response => response.StatusCode == HttpStatusCode.Created)
            .Select(response => response.Content.ReadAsStringAsync()));
        bodies.Distinct().Should().ContainSingle("every success returns the one task");
        (await CountAsync("Tasks")).Should().Be(1);
    }

    [Fact]
    public async Task Restore_WithTheETagFromBeforeTheDelete_Returns412AndKeepsItDeleted()
    {
        var id = await CreateAsync();
        var beforeDelete = (await _client.GetAsync($"/v2/Tasks/{id}")).Headers.ETag!;
        (await _client.DeleteAsync($"/v2/Tasks/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/Tasks/{id}/restore");
        request.Headers.IfMatch.Add(beforeDelete);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        (await _client.GetAsync($"/v2/Tasks/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Purge_RemovesOnlyTasksInTheTrashLongerThan30Days()
    {
        var old = await CreateAsync();
        (await _client.DeleteAsync($"/v2/Tasks/{old}")).EnsureSuccessStatusCode();
        _factory.Clock.Advance(TimeSpan.FromDays(2));
        var recent = await CreateAsync();
        (await _client.DeleteAsync($"/v2/Tasks/{recent}")).EnsureSuccessStatusCode();
        await CreateAsync();
        _factory.Clock.Advance(TimeSpan.FromDays(29));

        using var scope = _factory.Services.CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new PurgeDeletedTasksCommand());

        purged.Should().Be(1);
        (await CountAsync("Tasks")).Should().Be(2);
        using var trash = JsonDocument.Parse(await _client.GetStringAsync("/v2/Tasks?deleted=true"));
        trash.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())
            .Should().Equal(recent);
    }

    private async Task<Guid> CreateAsync()
    {
        var response = await _client.PostAsJsonAsync("/v2/Tasks", new { name = "Task" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> PostWithKeyAsync(string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/Tasks")
        {
            Content = new StringContent("""{ "name": "Offline task" }""", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
        };
        request.Headers.Add("Idempotency-Key", key);

        return _client.SendAsync(request);
    }

    private async Task<int> CountAsync(string table)
    {
        using var scope = _factory.Services.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<InsequensContext>().Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM [{table}]";

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
