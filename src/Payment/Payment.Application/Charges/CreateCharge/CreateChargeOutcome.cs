namespace Payment.Application.Charges.CreateCharge;

public enum CreateChargeKind
{
    Succeeded,
    Declined,
    IdempotencyConflict,
}

public sealed record CreateChargeOutcome(
    CreateChargeKind Kind,
    CreateChargeResult? Charge);