using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Api.Auth;
using Payment.Application.Charges.CreateCharge;

namespace Payment.Api.Charges;

[Authorize(Policy = PaymentAuth.ChargePolicy)]
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
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)
            || string.IsNullOrWhiteSpace(command.Email)
            || command.Card is null
            || string.IsNullOrWhiteSpace(command.Card.Number))
            return BadRequest();

        var outcome = await _createChargeHandler.Handle(command, cancellationToken);
        return outcome.Kind switch
        {
            CreateChargeKind.Succeeded => Ok(outcome.Charge),
            CreateChargeKind.Declined => StatusCode(StatusCodes.Status402PaymentRequired, outcome.Charge),
            CreateChargeKind.IdempotencyConflict => Conflict(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}