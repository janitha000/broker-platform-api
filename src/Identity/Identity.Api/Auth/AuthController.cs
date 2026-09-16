using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Broker.Hosting.Auth;
using Identity.Application.Abstractions;
using Identity.Application.Tenants.CompleteAuth0Login;
using Identity.Application.Tenants.Login;
using Identity.Application.Tenants.RegisterTenant;
using Identity.Domain.Tenants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Identity.Api.Auth;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterTenantHandler _registerTenantHandler;
    private readonly LoginHandler _loginHandler;
    private readonly CompleteAuth0LoginHandler _completeAuth0LoginHandler;
    private readonly Auth0Options _auth0;
    private readonly AuthOptions _auth;

    public AuthController(
        RegisterTenantHandler registerTenantHandler,
        LoginHandler loginHandler,
        CompleteAuth0LoginHandler completeAuth0LoginHandler,
        IOptions<Auth0Options> auth0,
        IOptions<AuthOptions> auth)
    {
        _registerTenantHandler = registerTenantHandler;
        _loginHandler = loginHandler;
        _completeAuth0LoginHandler = completeAuth0LoginHandler;
        _auth0 = auth0.Value;
        _auth = auth.Value;
    }

    [AllowAnonymous]
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
            RegisterTenantKind.IdentityProviderUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> PasswordLogin(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        if (_auth.UseAuth0Organizations)
            return StatusCode(StatusCodes.Status410Gone);

        var result = await _loginHandler.Handle(command, cancellationToken);
        if (result is null)
            return Unauthorized();

        AppendAccessCookie(result.AccessToken);
        return Ok(ToUser(result.TenantId, result.BrokerId, result.Email, result.Role));
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl, [FromQuery] string? organization)
    {
        var path = SafeReturnPath(returnUrl);
        var complete = $"{AppBaseUrl()}/auth/complete?returnUrl={Uri.EscapeDataString(path)}";
        var properties = new AuthenticationProperties { RedirectUri = complete };
        if (!string.IsNullOrWhiteSpace(organization))
            properties.Items["organization"] = organization.Trim();
        return Challenge(properties, Auth0Auth.ChallengeScheme);
    }

    [AllowAnonymous]
    [HttpGet("complete")]
    public async Task<IActionResult> Complete(
        [FromQuery] string? returnUrl,
        CancellationToken cancellationToken)
    {
        var oidc = await HttpContext.AuthenticateAsync(Auth0Auth.CookieScheme);
        if (!oidc.Succeeded || oidc.Principal is null)
            return Unauthorized();

        var accessToken = oidc.Properties?.GetTokenValue("access_token");

        var email = oidc.Principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? oidc.Principal.FindFirst(ClaimTypes.Email)?.Value
            ?? oidc.Principal.FindFirst("email")?.Value;
        var sub = oidc.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? oidc.Principal.FindFirst("sub")?.Value
            ?? string.Empty;

        await HttpContext.SignOutAsync(Auth0Auth.CookieScheme);

        if (string.IsNullOrEmpty(email))
            return Unauthorized();

        var result = await _completeAuth0LoginHandler.Handle(
            new CompleteAuth0LoginCommand(email, sub),
            cancellationToken);
        if (result is null)
            return Redirect($"{AppBaseUrl()}/register");

        if (_auth.UseAuth0Organizations)
        {
            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized();
            AppendAccessCookie(accessToken);
        }
        else
        {
            AppendAccessCookie(result.AccessToken);
        }

        return Redirect($"{AppBaseUrl()}{SafeReturnPath(returnUrl)}");
    }

    [AllowAnonymous]
    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name, AuthCookie.Delete(Request.IsHttps));
        await HttpContext.SignOutAsync(Auth0Auth.CookieScheme);

        var returnTo = Uri.EscapeDataString(AppBaseUrl());
        var url =
            $"https://{_auth0.Domain}/v2/logout?client_id={Uri.EscapeDataString(_auth0.ClientId)}&returnTo={returnTo}";
        return Redirect(url);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var brokerId = User.FindFirst("broker_id")?.Value
            ?? User.FindFirst("https://api.broker-platform.com/broker_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value
            ?? User.FindFirst("https://api.broker-platform.com/tenant_id")?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;
        var role = User.FindFirst(BrokerPermissions.RoleClaimType)?.Value
            ?? User.FindFirst("https://api.broker-platform.com/roles")?.Value
            ?? User.FindAll("roles").FirstOrDefault()?.Value;
        if (!Guid.TryParse(brokerId, out var broker)
            || !Guid.TryParse(tenantId, out var tenant)
            || string.IsNullOrEmpty(email))
            return Unauthorized();

        return Ok(ToUser(tenant, broker, email, role ?? string.Empty));
    }

    private IActionResult CreatedWithCookie(RegisterTenantResult result)
    {
        if (!_auth.UseAuth0Organizations)
            AppendAccessCookie(result.AccessToken);
        return Created(string.Empty, ToUser(result.TenantId, result.BrokerId, result.Email, result.Role));
    }

    private void AppendAccessCookie(string accessToken)
    {
        Response.Cookies.Append(
            AuthCookie.Name,
            accessToken,
            AuthCookie.Create(Request.IsHttps));
    }

    private string AppBaseUrl() => _auth0.AppBaseUrl.TrimEnd('/');

    private static string SafeReturnPath(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//"))
            return "/";
        return returnUrl;
    }

    private static AuthUserResponse ToUser(Guid tenantId, Guid brokerId, string email, string role) =>
        new(tenantId, brokerId, email, role);
}
