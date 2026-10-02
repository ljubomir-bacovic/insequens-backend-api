using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Insequens.Domain.Data;
using Insequens.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Insequens.Api.Tests.Controllers;

public class AuthControllerLoginTests
{
    private const string Email = "user@example.com";
    private const string Password = "Valid-Passw0rd";

    [Fact]
    public async Task Login_UnconfirmedUser_ReturnsUnauthorizedWithConfirmEmailMessage()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password, emailConfirmed: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        var response = await client.PostAsJsonAsync("/v1/Auth/login", new { Email, Password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Please confirm your email before logging in.");
    }

    [Fact]
    public async Task Login_ConfirmedUserWithValidPassword_ReturnsTokens()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password, emailConfirmed: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        var response = await client.PostAsJsonAsync("/v1/Auth/login", new { Email, Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private sealed class InsequensApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public async Task CreateUserAsync(string email, string password, bool emailConfirmed)
        {
            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = emailConfirmed,
            };

            var result = await userManager.CreateAsync(user, password);

            result.Succeeded.Should().BeTrue();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "integration-test-jwt-key-123456789012345",
                    ["Jwt:Issuer"] = "https://localhost:7269",
                    ["Jwt:Audience"] = "http://localhost:3000",
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
    }
}
