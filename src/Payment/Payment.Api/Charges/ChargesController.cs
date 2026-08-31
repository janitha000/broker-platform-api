using Microsoft.AspNetCore.Mvc;
using Payment.Application.Charges.CreateCharge;

namespace Payment.Api.Charges;

[ApiController]
[Route("payments")]
public sealed class ChargesController : ControllerBase
{
    private readonly CreateChargeHandler _createChargeHandler;

    public ChargesController(CreateChargeHandler createChargeHandler)
    {
        _createChargeHandler = createChargeHandler;
    }

    [HttpPost("charges")]
    public async Task<IActionResult> Charge(
        [FromBody] CreateChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _createChargeHandler.Handle(command, cancellationToken);
        return Ok(result);
    }
}
