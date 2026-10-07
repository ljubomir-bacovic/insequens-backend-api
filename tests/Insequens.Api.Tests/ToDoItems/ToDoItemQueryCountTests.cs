using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Identity;
using Insequens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Api.Tests.ToDoItems;

/// <summary>
/// The SQL each item endpoint sends, on a relational provider. Authorization and the handler share one load,
/// so a command is one SELECT plus its write, and a read is one SELECT.
/// </summary>
public sealed class ToDoItemQueryCountTests : IAsyncDisposable
{
    private readonly SqlCommandRecorder _recorder = new();
    private readonly InsequensApiFactory _factory;
    private readonly Guid _userId = Guid.NewGuid();

    public ToDoItemQueryCountTests()
    {
        _factory = new InsequensApiFactory(
            relationalDatabase: true,
            configureServices: services => services.AddSingleton<IInterceptor>(_recorder));
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task PatchName_OnOwnedItem_LoadsTheItemOnceAndUpdatesIt()
    {
        var itemId = await SeedAsync(_userId);
        using var client = CreateClient(_userId);

        var response = await client.PatchAsJsonAsync($"/v1/ToDoItem/{itemId}/name", "Renamed");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _recorder.Selects.Should().ContainSingle("the ownership check is the load the handler uses");
        _recorder.Commands.Should().ContainSingle(command => command.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase));
        _recorder.Commands.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_OnOwnedItem_RunsOneQuery()
    {
        var itemId = await SeedAsync(_userId);
        using var client = CreateClient(_userId);

        var response = await client.GetAsync($"/v1/ToDoItem/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _recorder.Commands.Should().ContainSingle().Which.Should().StartWith("SELECT");
    }

    [Fact]
    public async Task GetById_OnOtherUsersItem_Returns404AfterOneQuery()
    {
        var itemId = await SeedAsync(Guid.NewGuid());
        using var client = CreateClient(_userId);

        var response = await client.GetAsync($"/v1/ToDoItem/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _recorder.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task PatchName_OnOtherUsersItem_Returns404WithoutWriting()
    {
        var itemId = await SeedAsync(Guid.NewGuid());
        using var client = CreateClient(_userId);

        var response = await client.PatchAsJsonAsync($"/v1/ToDoItem/{itemId}/name", "Renamed");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _recorder.Commands.Should().ContainSingle().Which.Should().StartWith("SELECT");
    }

    private HttpClient CreateClient(Guid userId)
    {
        var client = _factory.CreateHttpsClient();
        client.UseBearer(_factory.CreateAccessToken(userId));
        return client;
    }

    private async Task<Guid> SeedAsync(Guid ownerId)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<InsequensContext>();
            var item = ToDoItem.Create(ownerId, "Task", null, null, null);
            context.Users.Add(new ApplicationUser { Id = ownerId, UserName = $"{ownerId}@example.com" });
            context.ToDoItems.Add(item);
            await context.SaveChangesAsync();
            _recorder.Clear();

            return item.Id;
        }
    }
}
