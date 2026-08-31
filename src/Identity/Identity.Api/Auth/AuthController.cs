using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Broker.Hosting.Auth;
using Identity.Application.Tenants.Login;
using Identity.Application.Tenants.RegisterTenant;
using Microsoft.AspNetCore.Authorization;
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
        if (string.IsNullOrWhiteSpace(command.Name)
            || string.IsNullOrWhiteSpace(command.Email)
            || string.IsNullOrWhiteSpace(command.Password)
            || command.Card is null
            || string.IsNullOrWhiteSpace(command.Card.Number))
            return BadRequest();

        var fromHeader = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var key = !string.IsNullOrWhiteSpace(fromHeader)
            ? fromHeader.Trim()
            : !string.IsNullOrWhiteSpace(command.IdempotencyKey)
                ? command.IdempotencyKey.Trim()
                : Guid.NewGuid().ToString("N");
        command = command with { IdempotencyKey = key };

        var outcome = await _registerTenantHandler.Handle(command, cancellationToken);
        return outcome.Kind switch
        {
            RegisterTenantKind.Succeeded => CreatedWithCookie(outcome.Result!),
            RegisterTenantKind.DuplicateEmail => Conflict(),
            RegisterTenantKind.PaymentConflict => Conflict(),
            RegisterTenantKind.PaymentDeclined => StatusCode(StatusCodes.Status402PaymentRequired),
            RegisterTenantKind.PaymentUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _loginHandler.Handle(command, cancellationToken);
        if (result is null)
            return Unauthorized();

        AppendAccessCookie(result.AccessToken);
        return Ok(ToUser(result.TenantId, result.BrokerId, result.Email));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name, AuthCookie.Delete(Request.IsHttps));
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var brokerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        if (!Guid.TryParse(brokerId, out var broker)
            || !Guid.TryParse(tenantId, out var tenant)
            || string.IsNullOrEmpty(email))
            return Unauthorized();

        return Ok(ToUser(tenant, broker, email));
    }

    private IActionResult CreatedWithCookie(RegisterTenantResult result)
    {
        AppendAccessCookie(result.AccessToken);
        return Created(string.Empty, ToUser(result.TenantId, result.BrokerId, result.Email));
    }

    private void AppendAccessCookie(string accessToken)
    {
        Response.Cookies.Append(
            AuthCookie.Name,
            accessToken,
            AuthCookie.Create(Request.IsHttps));
    }

    private static AuthUserResponse ToUser(Guid tenantId, Guid brokerId, string email) =>
        new(tenantId, brokerId, email);
}
