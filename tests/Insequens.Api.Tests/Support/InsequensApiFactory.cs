using Insequens.Application.Abstractions;
using Insequens.Infrastructure.Persistence;
using Insequens.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Abstractions.Email;

namespace Insequens.Api.Tests.Support;

/// <summary>
/// The API on an in-memory database with a fake clock and a capturing email sender. Each instance has
/// its own database and its own rate-limit counters.
/// </summary>
public sealed class InsequensApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "integration-test-jwt-key-123456789012345";
    public const string JwtIssuer = "https://localhost:7269";
    public const string JwtAudience = "http://localhost:3000";
    public const string FrontendBaseUrl = "http://localhost:3000";

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly Dictionary<string, string?> _settings;
    private readonly string _environment;
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly bool _captureEmails;

    /// <param name="startTime">Start of the fake clock; defaults to the real current time so tokens it issues also pass the JWT bearer handler.</param>
    /// <param name="captureEmails">Replace the MailKit sender with <see cref="EmailSender"/>.</param>
    public InsequensApiFactory(
        IReadOnlyDictionary<string, string?>? settings = null,
        string environment = "Development",
        Action<IServiceCollection>? configureServices = null,
        DateTimeOffset? startTime = null,
        bool captureEmails = true)
    {
        _settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = JwtKey,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience,
            ["Frontend:BaseUrl"] = FrontendBaseUrl,
        };

        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            _settings[key] = value;
        }

        _environment = environment;
        _configureServices = configureServices;
        _captureEmails = captureEmails;
        Clock = new FakeTimeProvider(startTime ?? DateTimeOffset.UtcNow);
    }

    /// <summary>Settings a Production host needs on top of <c>appsettings.json</c>.</summary>
    public static IReadOnlyDictionary<string, string?> ProductionSettings { get; } = new Dictionary<string, string?>
    {
        ["AllowedHosts"] = "api.insequens.test",
        ["Cors:AllowedOrigins:0"] = "https://app.insequens.test",
        ["Email:SmtpServer"] = "smtp.insequens.test",
        ["Email:From"] = "no-reply@insequens.test",
    };

    public FakeTimeProvider Clock { get; }

    public CapturingEmailSender EmailSender { get; } = new();

    public HttpClient CreateHttpsClient(string host = "localhost") => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri($"https://{host}"),
    });

    public async Task<ApplicationUser> CreateUserAsync(string email, string password, bool emailConfirmed = true)
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
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        return user;
    }

    public async Task<ApplicationUser?> FindUserAsync(string email)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        return await userManager.FindByEmailAsync(email);
    }

    public string CreateAccessToken(Guid userId) =>
        Services.GetRequiredService<ITokenService>().CreateAccessToken(userId, []).Value;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(_settings));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<InsequensContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<InsequensContext>>();
            services.RemoveAll<InsequensContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.AddInsequensContext(options => options.UseInMemoryDatabase(_databaseName));

            if (_captureEmails)
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(EmailSender);
            }

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            _configureServices?.Invoke(services);
        });
    }
}
