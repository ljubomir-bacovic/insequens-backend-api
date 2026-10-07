using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Options;

namespace Insequens.Api.Configuration;

/// <summary>Outside Development, <c>AllowedHosts</c> must name the API's hosts instead of allowing any host.</summary>
public sealed class AllowedHostsValidator(IHostEnvironment environment) : IValidateOptions<HostFilteringOptions>
{
    public ValidateOptionsResult Validate(string? name, HostFilteringOptions options)
    {
        if (environment.IsDevelopment())
        {
            return ValidateOptionsResult.Skip;
        }

        var hosts = options.AllowedHosts.Where(host => !string.IsNullOrWhiteSpace(host)).ToArray();

        return hosts.Length > 0 && !hosts.Contains("*")
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("AllowedHosts must list the API's host names outside Development, not '*'.");
    }
}
