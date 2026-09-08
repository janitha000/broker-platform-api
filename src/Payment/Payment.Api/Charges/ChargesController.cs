using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Api.Auth;
using Payment.Application.Charges.CreateCharge;
using Payment.Application.Charges.RefundCharge;

namespace Payment.Api.Charges;

[ApiController]
[Route("payments")]
public sealed class ChargesController : ControllerBase
{
    private readonly CreateChargeHandler _createChargeHandler;
    private readonly RefundChargeHandler _refundChargeHandler;

    public ChargesController(
        CreateChargeHandler createChargeHandler,
        RefundChargeHandler refundChargeHandler)
    {
        _createChargeHandler = createChargeHandler;
        _refundChargeHandler = refundChargeHandler;
    }

    [Authorize(Policy = PaymentAuth.ChargePolicy)]
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

    [Authorize(Policy = PaymentAuth.RefundPolicy)]
    [HttpPost("refunds")]
    public async Task<IActionResult> Refund(
        [FromBody] RefundChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ChargeId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return BadRequest();

        var outcome = await _refundChargeHandler.Handle(command, cancellationToken);
        return outcome.Kind switch
        {
            RefundChargeKind.Succeeded => Ok(new { chargeId = outcome.ChargeId, status = "Refunded" }),
            RefundChargeKind.NotFound => NotFound(),
            RefundChargeKind.NotRefundable => Conflict(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
