using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Contracts.V1.Tasks;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Api.Tests.ToDoItems;

public class ToDoItemDomainRulesTests
{
    [Fact]
    public async Task Create_WithBearerToken_RecordsTheCallerAsCreatorAndEditor()
    {
        var userId = Guid.NewGuid();
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(userId));

        var response = await client.PostAsJsonAsync("/v1/ToDoItem", new ToDoItemCreateModel("Task", null, 0, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ToDoItemGetDetailsModel>();
        var item = await FindAsync(factory, created!.Id);
        item.CreatedBy.Should().Be(userId);
        item.UpdatedBy.Should().Be(userId);
        item.CreatedOn.Should().Be(factory.Clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task UpdateDescription_OverMaximumLength_Returns400AndKeepsDescription()
    {
        var userId = Guid.NewGuid();
        await using var factory = new InsequensApiFactory();
        var itemId = await SeedAsync(factory, ToDoItem.Create(userId, "Task", "Original", null, null));
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(userId));

        var response = await client.PatchAsJsonAsync(
            $"/v1/ToDoItem/{itemId}/description",
            new string('a', ToDoItem.DescriptionMaxLength + 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").GetProperty("Description")[0].GetString()
            .Should().Be("Task description must not exceed 4000 characters.");
        (await FindAsync(factory, itemId)).Description.Should().Be("Original");
    }

    [Fact]
    public async Task Create_WithDescriptionOver4000Characters_Returns400()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PostAsJsonAsync(
            "/v1/ToDoItem",
            new ToDoItemCreateModel("Task", new string('a', ToDoItem.DescriptionMaxLength + 1), 0, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").GetProperty("Description")[0].GetString()
            .Should().Be("Task description must not exceed 4000 characters.");
    }

    [Fact]
    public async Task UpdateDueDate_WithNull_ClearsTheDueDateAndTheListReturnsNull()
    {
        var userId = Guid.NewGuid();
        await using var factory = new InsequensApiFactory();
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        var itemId = await SeedAsync(factory, ToDoItem.Create(userId, "Task", null, null, today));
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(userId));

        var response = await client.PatchAsync(
            $"/v1/ToDoItem/{itemId}/duedate",
            new StringContent("null", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await FindAsync(factory, itemId)).DueDate.Should().BeNull();
        using var list = JsonDocument.Parse(await client.GetStringAsync("/v1/ToDoItem"));
        list.RootElement.GetProperty("items")[0].GetProperty("dueDate").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task UpdateDueDate_MoreThanTenYearsAhead_Returns400AndKeepsTheDueDate()
    {
        var userId = Guid.NewGuid();
        await using var factory = new InsequensApiFactory();
        var itemId = await SeedAsync(factory, ToDoItem.Create(userId, "Task", null, null, null));
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(userId));

        var tooLate = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddYears(10).AddDays(1);
        var response = await client.PatchAsJsonAsync($"/v1/ToDoItem/{itemId}/duedate", tooLate);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await FindAsync(factory, itemId)).DueDate.Should().BeNull();
    }

    private static async Task<Guid> SeedAsync(InsequensApiFactory factory, ToDoItem item)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InsequensContext>();
        context.ToDoItems.Add(item);
        await context.SaveChangesAsync();

        return item.Id;
    }

    private static async Task<ToDoItem> FindAsync(InsequensApiFactory factory, Guid itemId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InsequensContext>();

        return await context.ToDoItems.AsNoTracking().SingleAsync(item => item.Id == itemId);
    }
}
