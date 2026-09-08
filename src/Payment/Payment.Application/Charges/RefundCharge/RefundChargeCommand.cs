namespace Payment.Application.Charges.RefundCharge;

public sealed record RefundChargeCommand(Guid ChargeId, string IdempotencyKey);
