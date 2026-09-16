namespace Identity.Application.Abstractions;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public const string IdentityJwt = "IdentityJwt";
    public const string Auth0Organizations = "Auth0Organizations";

    public string Mode { get; set; } = IdentityJwt;

    public bool UseAuth0Organizations =>
        string.Equals(Mode, Auth0Organizations, StringComparison.OrdinalIgnoreCase);
}