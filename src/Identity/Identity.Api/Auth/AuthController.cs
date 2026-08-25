using Identity.Application.Tenants.Login;
using Identity.Application.Tenants.RegisterTenant;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Auth;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterTenantHandler _registerTenantHandler;
    private readonly LoginHandler _loginHandler;

    public AuthController(
        RegisterTenantHandler registerTenantHandler,
        LoginHandler loginHandler)
    {
        _registerTenantHandler = registerTenantHandler;
        _loginHandler = loginHandler;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _registerTenantHandler.Handle(command, cancellationToken);
        if (result is null)
            return Conflict();

        return Created(string.Empty, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _loginHandler.Handle(command, cancellationToken);
        if (result is null)
            return Unauthorized();

        return Ok(result);
    }
}
