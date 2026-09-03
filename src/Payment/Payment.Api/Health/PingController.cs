using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Payment.Api.Health;

[AllowAnonymous]
[ApiController]
[Route("health")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping() => Ok(new { status = "OK" });
}
