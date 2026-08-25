namespace Identity.Application.Tenants.Login;

public sealed record LoginCommand(
    string Email,
    string Password);
