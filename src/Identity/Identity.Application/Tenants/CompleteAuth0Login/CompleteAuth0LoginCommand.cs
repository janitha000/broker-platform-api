namespace Identity.Application.Tenants.CompleteAuth0Login;

public sealed record CompleteAuth0LoginCommand(string Email, string Auth0Sub);
