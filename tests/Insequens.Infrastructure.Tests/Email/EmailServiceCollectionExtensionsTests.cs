using FluentAssertions;
using Insequens.Domain.ServiceContracts;
using Insequens.Infrastructure.DataAccess.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Insequens.Infrastructure.Tests.Email;

public class EmailServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEmailSender_WithValidConfiguration_ResolvesMailKitEmailSenderAndBindsOptions()
    {
        using var serviceProvider = BuildServiceProvider(ValidConfiguration());

        serviceProvider.GetRequiredService<IEmailSender>().Should().BeOfType<MailKitEmailSender>();
        serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value.Should().Be(new EmailOptions
        {
            SmtpServer = "localhost",
            Port = 1025,
            UseTls = false,
            From = "no-reply@localhost",
        });
    }

    [Theory]
    [InlineData("Email:SmtpServer", "", "SmtpServer")]
    [InlineData("Email:Port", "0", "Port")]
    [InlineData("Email:Port", "65536", "Port")]
    [InlineData("Email:From", "", "From")]
    [InlineData("Email:From", "not-an-email-address", "From")]
    public void AddEmailSender_WithInvalidSetting_FailsValidation(string key, string value, string expectedMember)
    {
        var configuration = ValidConfiguration();
        configuration[key] = value;
        using var serviceProvider = BuildServiceProvider(configuration);

        var action = () => serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;

        action.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle(failure => failure.Contains(expectedMember));
    }

    [Fact]
    public void AddEmailSender_WithUsernamePasswordAndTls_PassesValidation()
    {
        var configuration = ValidConfiguration();
        configuration["Email:Username"] = "smtp-user";
        configuration["Email:Password"] = "smtp-password";
        configuration["Email:UseTls"] = "true";
        using var serviceProvider = BuildServiceProvider(configuration);

        var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;

        options.Username.Should().Be("smtp-user");
    }

    [Theory]
    [InlineData("", "true", "Password")]
    [InlineData("smtp-password", "false", "UseTls")]
    public void AddEmailSender_WithUsernameAndInvalidCredentialSettings_FailsValidation(
        string password,
        string useTls,
        string expectedMember)
    {
        var configuration = ValidConfiguration();
        configuration["Email:Username"] = "smtp-user";
        configuration["Email:Password"] = password;
        configuration["Email:UseTls"] = useTls;
        using var serviceProvider = BuildServiceProvider(configuration);

        var action = () => serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;

        action.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle(failure => failure.Contains(expectedMember));
    }

    private static Dictionary<string, string?> ValidConfiguration() => new()
    {
        ["Email:SmtpServer"] = "localhost",
        ["Email:Port"] = "1025",
        ["Email:UseTls"] = "false",
        ["Email:From"] = "no-reply@localhost",
    };

    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmailSender(configuration);

        return services.BuildServiceProvider();
    }
}
