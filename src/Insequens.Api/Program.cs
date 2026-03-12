using Insequens.Domain.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Insequens.Domain.DataAccess;
using Insequens.Domain.ServiceContracts;
using Insequens.Core.Profiles;
using Insequens.Core.Services;
using Insequens.Api;
using Insequens.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Insequens.Infrastructure.Data.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load environment-specific configurations
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode.");

// Add services to the container.

var myPolicyName = "MyPolicyName"; 
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myPolicyName,
      configurePolicy: policy =>
      {
          policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
      });
});
var dataConnectionString = builder.Configuration["ConnectionStrings:InsequensConnection"];

builder.Services.AddScoped<IDataContext, DataContext>();
builder.Services.AddAutoMapper(typeof(ToDoItemProfile));
builder.Services.AddScoped<IToDoItemService, ToDoItemService>();

builder.Services.AddDbContextPool<InsequensContext>(options =>
    options.UseSqlServer(dataConnectionString,
            providerOptions => providerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero
    };
});
/*
builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
});

builder.Services.AddAuthorization();

/**/
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<InsequensContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // e.g. options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<InsequensContext>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddControllers();
builder.Services.AddHttpClient();
//builder.Services.AddHostedService<WarmKeeper>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/app-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(myPolicyName);

//app.MapIdentityApi<ApplicationUser>();

/*

app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync().ConfigureAwait(false);
});
*/

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();