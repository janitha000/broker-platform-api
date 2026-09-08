using Payment.Domain.Charges;

namespace Payment.Application.Charges.RefundCharge;

public sealed class RefundChargeHandler
{
    private readonly IChargeRepository _charges;

    public RefundChargeHandler(IChargeRepository charges)
    {
        _charges = charges;
    }

    public async Task<RefundChargeOutcome> Handle(
        RefundChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        var charge = await _charges.GetById(command.ChargeId, cancellationToken);
        if (charge is null)
            return new RefundChargeOutcome(RefundChargeKind.NotFound, null);

        if (charge.Status == ChargeStatus.Refunded)
            return new RefundChargeOutcome(RefundChargeKind.Succeeded, charge.Id);

        if (charge.Status != ChargeStatus.Succeeded)
            return new RefundChargeOutcome(RefundChargeKind.NotRefundable, charge.Id);

        charge.Status = ChargeStatus.Refunded;
        charge.RefundedAt = DateTime.UtcNow;
        await _charges.Update(charge, cancellationToken);
        return new RefundChargeOutcome(RefundChargeKind.Succeeded, charge.Id);
    }
}
