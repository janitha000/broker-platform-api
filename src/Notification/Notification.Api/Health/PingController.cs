using Microsoft.AspNetCore.Mvc;

namespace Notification.Api.Health;

[ApiController]
[Route("health")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping() => Ok(new { status = "OK" });
}
