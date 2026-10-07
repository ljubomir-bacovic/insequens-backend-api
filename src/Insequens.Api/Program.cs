using Insequens.Api;
using Insequens.Api.Configuration;
using Insequens.Api.RateLimiting;
using Insequens.Api.Security;
using Insequens.Application;
using Insequens.Application.Options;
using Insequens.Infrastructure.Email;
using Insequens.Infrastructure.Identity;
using Insequens.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode.");

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = RequestLimits.MaxRequestBodySize);
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = RequestLimits.MaxRequestBodySize);

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddPersistence(builder.Configuration.GetConnectionString("InsequensConnection"));
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddEmailSender(builder.Configuration);
builder.Services.AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<AccountDeletionOptions>()
    .Bind(builder.Configuration.GetSection(AccountDeletionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtBearerSecurityDocumentTransformer>();
});

builder.Services.AddApplication();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/app-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(ConfigureCorsPolicy.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Insequens API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    }).AllowAnonymous();

    // Redirect root path to Scalar API documentation in development
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .AllowAnonymous()
        .ExcludeFromDescription();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
