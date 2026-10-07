using Insequens.Application.Abstractions.Email;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Account;

/// <summary>Substitutes for the user store and email, registered on top of <see cref="Support.TestDbContextFactory"/>.</summary>
internal sealed class AccountTestServices
{
    public const string FrontendBaseUrl = "https://app.example.com";

    public IIdentityService IdentityService { get; } = Substitute.For<IIdentityService>();

    public IEmailSender EmailSender { get; } = Substitute.For<IEmailSender>();

    public void Register(IServiceCollection services)
    {
        services.AddSingleton(IdentityService);
        services.AddSingleton(EmailSender);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new FrontendOptions { BaseUrl = FrontendBaseUrl }));
    }

    public void PasswordCheckReturns(Guid userId, string password, PasswordSignInStatus status) =>
        IdentityService.CheckPasswordSignInAsync(userId, password, Arg.Any<CancellationToken>()).Returns(status);
}
