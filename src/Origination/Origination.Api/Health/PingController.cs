using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Origination.Api.Health;

[AllowAnonymous]
[ApiController]
[Route("health")]
[DisableRateLimiting]

public sealed class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping() => Ok(new { status = "OK" });
}
