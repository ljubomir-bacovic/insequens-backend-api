using FluentAssertions;
using Insequens.Domain.Data;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace Insequens.Api.Tests.Queries;

public class GetToDoItemEndpointTests
{
    [Fact]
    public async Task GetToDoItem_WhenCalledByOwner_ReturnsItemDetails()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        await using var factory = new InsequensApiFactory(userId, itemId);
        await factory.InitializeAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateAccessToken(userId));

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

    private sealed class InsequensApiFactory(Guid userId, Guid itemId) : WebApplicationFactory<Program>
    {
        private const string JwtKey = "integration-test-jwt-key-123456789012345";
        private const string JwtIssuer = "https://localhost:7269";
        private const string JwtAudience = "http://localhost:3000";
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<InsequensContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<InsequensContext>>();
                services.RemoveAll<InsequensContext>();
                services.AddDbContextPool<InsequensContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }

        public async Task InitializeAsync()
        {
            using var scope = Services.CreateScope();
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

        public string CreateAccessToken(Guid authenticatedUserId)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience,
                claims:
                [
                    new Claim(ClaimTypes.NameIdentifier, authenticatedUserId.ToString()),
                ],
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
