using Insequens.Domain.ServiceContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISmtpClientFactory, SmtpClientFactory>();
        services.AddTransient<IEmailSender, MailKitEmailSender>();

        return services;
    }
}
