namespace Payment.Application.Charges.RefundCharge;

public enum RefundChargeKind
{
    Succeeded,
    NotFound,
    NotRefundable,
}

public sealed record RefundChargeOutcome(RefundChargeKind Kind, Guid? ChargeId);
