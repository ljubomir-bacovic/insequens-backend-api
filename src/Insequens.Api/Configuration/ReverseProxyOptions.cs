using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Insequens.Api.Configuration;

/// <summary>
/// The proxies whose <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> headers are trusted. When both
/// lists are empty the headers are ignored and the client IP is the connection's remote address.
/// </summary>
public sealed record ReverseProxyOptions : IValidatableObject
{
    public const string SectionName = "ReverseProxy";

    public IList<string> KnownProxies { get; init; } = [];

    /// <summary>CIDR ranges, for example a load balancer subnet.</summary>
    public IList<string> KnownNetworks { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var proxy in KnownProxies.Where(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            yield return new ValidationResult($"ReverseProxy:KnownProxies contains an invalid IP address: {proxy}.", [nameof(KnownProxies)]);
        }

        foreach (var network in KnownNetworks.Where(network => !IPNetwork.TryParse(network, out _)))
        {
            yield return new ValidationResult($"ReverseProxy:KnownNetworks contains an invalid CIDR range: {network}.", [nameof(KnownNetworks)]);
        }
    }
}
