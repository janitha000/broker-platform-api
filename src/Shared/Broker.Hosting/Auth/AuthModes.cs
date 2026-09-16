namespace Broker.Hosting.Auth;

public static class AuthModes
{
    public const string SectionName = "Auth";
    public const string IdentityJwt = "IdentityJwt";
    public const string Auth0Organizations = "Auth0Organizations";

    public static string GetMode(IConfiguration configuration)
    {
        var mode = configuration[$"{SectionName}:Mode"];
        return string.IsNullOrWhiteSpace(mode) ? IdentityJwt : mode.Trim();
    }

    public static bool UseAuth0Organizations(IConfiguration configuration) =>
        string.Equals(GetMode(configuration), Auth0Organizations, StringComparison.OrdinalIgnoreCase);
}
