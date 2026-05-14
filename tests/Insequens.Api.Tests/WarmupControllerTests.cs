using Insequens.Api.Controllers;
using Insequens.Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Api.Tests;

public class WarmupControllerTests
{
    [Fact]
    public async Task Get_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        await using var context = CreateReachableContext();
        await context.Database.EnsureCreatedAsync();
        var controller = new WarmupController(context);

        var result = await controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Healthy", okResult.Value);
    }

    [Fact]
    public async Task Get_WhenDatabaseIsUnavailable_ReturnsServiceUnavailable()
    {
        await using var context = CreateUnavailableContext();
        var controller = new WarmupController(context);

        var result = await controller.Get();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
        Assert.Equal("Database unavailable", objectResult.Value);
    }

    private static InsequensContext CreateReachableContext()
    {
        var options = new DbContextOptionsBuilder<InsequensContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InsequensContext(options)
        {
            ToDoItems = null!
        };
    }

    private static InsequensContext CreateUnavailableContext()
    {
        var options = new DbContextOptionsBuilder<InsequensContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=WarmupControllerTests;User Id=sa;Password=Password123!;Connect Timeout=1;Encrypt=False;TrustServerCertificate=True")
            .Options;

        return new InsequensContext(options)
        {
            ToDoItems = null!
        };
    }
}
