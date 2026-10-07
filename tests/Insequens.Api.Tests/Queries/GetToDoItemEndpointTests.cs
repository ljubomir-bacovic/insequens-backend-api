using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Contracts.V1.Tasks;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Api.Tests.Queries;

public class GetToDoItemEndpointTests
{
    [Fact]
    public async Task GetToDoItem_WhenCalledByOwner_ReturnsItemDetails()
    {
        var userId = Guid.NewGuid();
        await using var factory = new InsequensApiFactory();
        var itemId = await SeedItemAsync(factory, userId);
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(userId));

        var response = await client.GetAsync($"/v1/ToDoItem/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ToDoItemGetDetailsModel>();
        result.Should().Be(new ToDoItemGetDetailsModel(
            itemId,
            "Projected item",
            "Projected description",
            TaskPriority.Medium,
            new DateOnly(2026, 7, 3),
            true));
    }

    private static async Task<Guid> SeedItemAsync(InsequensApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InsequensContext>();
        var item = ToDoItem.Create(userId, "Projected item", "Projected description", DomainPriority.Medium, new DateOnly(2026, 7, 3));
        item.MarkCompleted();
        context.ToDoItems.Add(item);

        await context.SaveChangesAsync();

        return item.Id;
    }
}
