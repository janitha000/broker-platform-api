
using Microsoft.AspNetCore.Mvc;

namespace Origination.Api.Health;

[ApiController]
[Route("health")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping() => Ok(new { status = "OK" });
}