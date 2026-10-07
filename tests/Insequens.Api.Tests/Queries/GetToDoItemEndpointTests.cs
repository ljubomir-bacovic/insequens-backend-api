using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Infrastructure.Persistence;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Insequens.Api.Tests.Queries;

public class GetToDoItemEndpointTests
{
    [Fact]
    public async Task GetToDoItem_WhenCalledByOwner_ReturnsItemDetails()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        await using var factory = new InsequensApiFactory();
        await SeedItemAsync(factory, userId, itemId);
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

    private static async Task SeedItemAsync(InsequensApiFactory factory, Guid userId, Guid itemId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InsequensContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        context.ToDoItems.Add(new ToDoItem
        {
            Id = itemId,
            UserId = userId,
            Name = "Projected item",
            Description = "Projected description",
            Priority = TaskPriority.Medium,
            DueDate = new DateOnly(2026, 7, 3),
            IsCompleted = true,
        });

        await context.SaveChangesAsync();
    }
}
