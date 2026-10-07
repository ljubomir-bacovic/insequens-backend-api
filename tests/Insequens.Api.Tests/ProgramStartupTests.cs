using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Insequens.Api.Tests.Support;
using Insequens.Application.Abstractions;
using Insequens.Application.Behaviors;
using Insequens.Application.Authorization;
using Insequens.Application.Exceptions;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Identity;
using Insequens.Infrastructure.Email;
using Insequens.Infrastructure.Persistence;
using Insequens.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Insequens.Application.Abstractions.Email;

namespace Insequens.Api.Tests;

public class ProgramStartupTests
{
    [Fact]
    public void Startup_RegistersApplicationServices()
    {
        using var factory = new InsequensApiFactory();
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        serviceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ISender>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IPublisher>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IMapper>().Should().NotBeNull();
        serviceProvider.GetRequiredService<AutoMapper.IConfigurationProvider>().Should().NotBeNull();
    }

    [Fact]
    public async Task Startup_DefaultAuthenticationSchemes_AreBearer()
    {
        using var factory = new InsequensApiFactory();
        var schemeProvider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

        var authenticateScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        var challengeScheme = await schemeProvider.GetDefaultChallengeSchemeAsync();

        authenticateScheme!.Name.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        challengeScheme!.Name.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public async Task Startup_IdentityRegistration_DoesNotRegisterCookieSchemes()
    {
        using var factory = new InsequensApiFactory();
        var schemeProvider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

        var schemes = await schemeProvider.GetAllSchemesAsync();

        schemes.Select(scheme => scheme.Name).Should().Equal(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void Startup_IdentityOptions_RequireConfirmedEmailAndUniqueEmail()
    {
        using var factory = new InsequensApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;

        options.SignIn.RequireConfirmedEmail.Should().BeTrue();
        options.User.RequireUniqueEmail.Should().BeTrue();
        options.Password.RequiredLength.Should().Be(8);
    }

    [Fact]
    public void Startup_RegistersIdentityServices()
    {
        using var factory = new InsequensApiFactory();
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        serviceProvider.GetRequiredService<UserManager<ApplicationUser>>().Should().NotBeNull();
        serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>().Should().NotBeNull();
        serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>().Should().NotBeNull();
    }

    [Fact]
    public void Startup_Configuration_LoadsEachAppSettingsFileOnce()
    {
        using var factory = new InsequensApiFactory();
        var configuration = (IConfigurationRoot)factory.Services.GetRequiredService<IConfiguration>();

        var appSettingsPaths = configuration.Providers
            .OfType<JsonConfigurationProvider>()
            .Select(provider => provider.Source.Path)
            .Where(path => path?.StartsWith("appsettings", StringComparison.Ordinal) == true)
            .ToArray();

        appSettingsPaths.Should().Equal("appsettings.json", "appsettings.Development.json");
    }

    [Fact]
    public void Startup_RegistersMailKitEmailSender()
    {
        using var factory = new InsequensApiFactory(captureEmails: false);
        using var scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<MailKitEmailSender>();
    }

    [Theory]
    [InlineData("Email:SmtpServer", "")]
    [InlineData("Email:Port", "0")]
    [InlineData("Email:From", "")]
    public void Startup_InvalidEmailOptions_FailsOnStart(string key, string value)
    {
        using var factory = new InsequensApiFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value });
                });
            });

        var action = () => factory.Services;

        action.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short-for-hmac-sha256")]
    public void Startup_InvalidJwtKey_FailsOnStartNamingTheSetting(string key)
    {
        using var factory = new InsequensApiFactory(new Dictionary<string, string?> { ["Jwt:Key"] = key });

        var action = () => factory.Services;

        action.Should().Throw<OptionsValidationException>().WithMessage("*Jwt:Key*");
    }

    [Theory]
    [InlineData("Jwt:Issuer", "", "Issuer")]
    [InlineData("Jwt:Audience", "", "Audience")]
    [InlineData("Jwt:AccessTokenLifetime", "00:00:00", "AccessTokenLifetime")]
    [InlineData("Frontend:BaseUrl", "", "BaseUrl")]
    [InlineData("Frontend:BaseUrl", "not-a-url", "BaseUrl")]
    [InlineData("AccountDeletion:GracePeriod", "366.00:00:00", "GracePeriod")]
    [InlineData("RateLimiting:Auth:PermitLimit", "0", "PermitLimit")]
    [InlineData("RateLimiting:Write:ReplenishmentPeriod", "00:00:00", "ReplenishmentPeriod")]
    [InlineData("ReverseProxy:KnownProxies:0", "not-an-ip", "KnownProxies")]
    [InlineData("ReverseProxy:KnownNetworks:0", "192.0.2.0/99", "KnownNetworks")]
    public void Startup_InvalidSetting_FailsOnStartNamingTheSetting(string key, string value, string expectedName)
    {
        using var factory = new InsequensApiFactory(new Dictionary<string, string?> { [key] = value });

        var action = () => factory.Services;

        action.Should().Throw<OptionsValidationException>().WithMessage($"*{expectedName}*");
    }

    [Fact]
    public void Startup_JwtKeyAndKeysBothSet_FailsOnStart()
    {
        using var factory = new InsequensApiFactory(new Dictionary<string, string?>
        {
            ["Jwt:Keys:0:Id"] = "a",
            ["Jwt:Keys:0:Secret"] = InsequensApiFactory.JwtKey,
        });

        var action = () => factory.Services;

        action.Should().Throw<OptionsValidationException>().WithMessage("*either Jwt:Key or Jwt:Keys*");
    }

    [Fact]
    public void Startup_JwtKeysAllInTheFuture_FailsOnStart()
    {
        using var factory = new InsequensApiFactory(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = string.Empty,
            ["Jwt:Keys:0:Id"] = "a",
            ["Jwt:Keys:0:Secret"] = InsequensApiFactory.JwtKey,
            ["Jwt:Keys:0:ActiveFrom"] = DateTimeOffset.UtcNow.AddDays(1).ToString("O"),
        });

        var action = () => factory.Services;

        action.Should().Throw<OptionsValidationException>().WithMessage("*ActiveFrom*");
    }

    [Fact]
    public void Startup_InProductionWithRequiredSettings_Starts()
    {
        using var factory = new InsequensApiFactory(InsequensApiFactory.ProductionSettings, environment: "Production");

        var action = () => factory.Services;

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("Cors:AllowedOrigins:0", "", "Cors:AllowedOrigins")]
    [InlineData("Cors:AllowedOrigins:0", "app.insequens.test", "Cors:AllowedOrigins")]
    [InlineData("AllowedHosts", "*", "AllowedHosts")]
    [InlineData("AllowedHosts", "", "AllowedHosts")]
    public void Startup_InProductionWithUnsafeSetting_FailsOnStart(string key, string value, string expectedName)
    {
        var settings = new Dictionary<string, string?>(InsequensApiFactory.ProductionSettings) { [key] = value };
        using var factory = new InsequensApiFactory(settings, environment: "Production");

        var action = () => factory.Services;

        action.Should().Throw<OptionsValidationException>().WithMessage($"*{expectedName}*");
    }

    [Fact]
    public void Startup_InDevelopmentWithoutCorsOrigins_Starts()
    {
        using var factory = new InsequensApiFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = string.Empty,
            ["Cors:AllowedOrigins:1"] = string.Empty,
        });

        var action = () => factory.Services;

        action.Should().NotThrow();
    }

    [Fact]
    public void Startup_RegistersApplicationDbContextWithAuditInterceptor()
    {
        using var factory = new InsequensApiFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        dbContext.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<InsequensContext>());
        scope.ServiceProvider.GetServices<IInterceptor>()
            .Should().ContainSingle().Which.Should().BeOfType<AuditableEntityInterceptor>();
    }

    [Fact]
    public async Task Startup_RegistersApplicationPipelineBehaviors()
    {
        var trace = new ExecutionTrace();
        using var factory = new InsequensApiFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(trace);
                    services.AddTransient<IRequestHandler<TestOwnedRequest, string>, TestOwnedRequestHandler>();
                    services.AddTransient<IValidator<TestOwnedRequest>, TestOwnedRequestValidator>();
                });
            });
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();
        var item = ToDoItem.Create(Guid.NewGuid(), "Task", null, null, null);
        dbContext.ToDoItems.Add(item);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TestOwnedRequest, string>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
            .ToArray();

        behaviors.Should().Equal(
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(AuthorizationBehavior<,>));

        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new TestOwnedRequest(item.UserId, item.Id, "example"));
        var otherUsersRequest = () => mediator.Send(new TestOwnedRequest(Guid.NewGuid(), item.Id, "example"));

        response.Should().Be("handled:example");
        trace.Steps.Should().Equal("validation", "handler");
        await otherUsersRequest.Should().ThrowAsync<NotFoundException>();
    }

    private sealed class ExecutionTrace
    {
        public List<string> Steps { get; } = [];
    }

    private sealed record TestOwnedRequest(Guid UserId, Guid ResourceId, string Name) : IRequest<string>, IOwned<ToDoItem>;

    private sealed class TestOwnedRequestHandler(ExecutionTrace trace) : IRequestHandler<TestOwnedRequest, string>
    {
        public Task<string> Handle(TestOwnedRequest request, CancellationToken cancellationToken)
        {
            trace.Steps.Add("handler");
            return Task.FromResult($"handled:{request.Name}");
        }
    }

    private sealed class TestOwnedRequestValidator : AbstractValidator<TestOwnedRequest>
    {
        public TestOwnedRequestValidator(ExecutionTrace trace)
        {
            RuleFor(request => request.Name).Must(name =>
            {
                trace.Steps.Add("validation");
                return !string.IsNullOrWhiteSpace(name);
            });
        }
    }
}
