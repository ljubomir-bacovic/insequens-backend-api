using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Domain.Data;
using Insequens.Domain.ServiceContracts;
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
using NSubstitute;

namespace Insequens.Api.Tests.Controllers;

public class AuthControllerEmailTests
{
    private const string Email = "user@example.com";
    private const string Password = "Valid-Passw0rd";

    [Fact]
    public async Task Register_NewUser_SendsConfirmationEmailToUser()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        EmailMessage? sentMessage = null;
        factory.EmailSender
            .When(sender => sender.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => sentMessage = callInfo.Arg<EmailMessage>());

        var response = await client.PostAsJsonAsync("/v1/Auth/register", new { Email, Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.EmailSender.Received(1).SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        sentMessage.Should().NotBeNull();
        sentMessage!.To.Should().Be(Email);
        sentMessage.Subject.Should().Be("Please confirm your registration");
        sentMessage.HtmlBody.Should().Contain("http://localhost:3000/confirm-email?userId=");
        sentMessage.TextBody.Should().BeNull();
    }

    [Fact]
    public async Task Register_ExistingEmail_DoesNotSendEmail()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Auth/register", new { Email, Password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await factory.EmailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForgotPassword_ExistingUser_SendsResetEmailToUser()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        EmailMessage? sentMessage = null;
        factory.EmailSender
            .When(sender => sender.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => sentMessage = callInfo.Arg<EmailMessage>());

        var response = await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.EmailSender.Received(1).SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        sentMessage.Should().NotBeNull();
        sentMessage!.To.Should().Be(Email);
        sentMessage.Subject.Should().Be("Password Reset");
        sentMessage.HtmlBody.Should().Contain("http://localhost:3000/reset-password?token=");
        sentMessage.HtmlBody.Should().Contain($"&email={Uri.EscapeDataString(Email)}");
        sentMessage.TextBody.Should().BeNull();
    }

    [Fact]
    public async Task ForgotPassword_UnknownUser_DoesNotSendEmail()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await factory.EmailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    private sealed class InsequensApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public IEmailSender EmailSender { get; } = Substitute.For<IEmailSender>();

        public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        public async Task CreateUserAsync(string email, string password)
        {
            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
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
                services.RemoveAll<IEmailSender>();
                services.AddSingleton(EmailSender);
            });
        }
    }
}
