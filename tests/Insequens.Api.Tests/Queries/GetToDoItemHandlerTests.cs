using AutoMapper;
using FluentAssertions;
using Insequens.Application;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.Data;
using Insequens.Domain.Entities;
using Insequens.Domain.Types;
using Insequens.Infrastructure.Data.Models;
using Insequens.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Insequens.Api.Tests.Queries;

public class GetToDoItemHandlerTests
{
    [Fact]
    public async Task Handle_WithOwnedItem_ReturnsProjectedDetails()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        await using var context = CreateContext();
        await SeedItemAsync(context, userId, itemId);
        using var dataContext = new DataContext(context);
        var handler = new GetToDoItemHandler(dataContext, CreateMapper());

        var result = await handler.Handle(new GetToDoItemQuery(itemId, userId), CancellationToken.None);

        result.Id.Should().Be(itemId);
        result.Name.Should().Be("Projected item");
        result.Description.Should().Be("Projected description");
        result.Priority.Should().Be(TaskPriority.Medium);
        result.DueDate.Should().Be(new DateOnly(2026, 7, 3));
        result.IsCompleted.Should().BeTrue();
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static TestInsequensContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InsequensContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestInsequensContext(options);
    }

    private static async Task SeedItemAsync(InsequensContext context, Guid userId, Guid itemId)
    {
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

    private sealed class TestInsequensContext : InsequensContext
    {
        [SetsRequiredMembers]
        public TestInsequensContext(DbContextOptions<InsequensContext> options)
            : base(options)
        {
            ToDoItems = Set<ToDoItem>();
        }
    }
}
