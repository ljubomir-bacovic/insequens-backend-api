namespace Insequens.Application.Commands.Auth;

internal static class FrontendLink
{
    public static string Build(string baseUrl, string path, params (string Name, string Value)[] query)
    {
        var queryString = string.Join(
            "&",
            query.Select(parameter => $"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(parameter.Value)}"));

        return $"{baseUrl.TrimEnd('/')}/{path}?{queryString}";
    }
}
