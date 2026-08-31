namespace Payment.Application.Charges.CreateCharge;

public sealed record CardDetails(
    string Number,
    int ExpMonth,
    int ExpYear,
    string Cvc);

public sealed record CreateChargeCommand(
    string Email,
    CardDetails Card,
    string IdempotencyKey);
