namespace Identity.Application.Abstractions;

public enum PaymentChargeStatus
{
    Succeeded,
    Declined,
    Conflict,
    Unavailable,
}

public enum PaymentRefundStatus
{
    Succeeded,
    NotFound,
    NotRefundable,
    Unavailable,
}

public sealed record PaymentCard(
    string Number,
    int ExpMonth,
    int ExpYear,
    string Cvc);

public sealed record PaymentChargeResult(PaymentChargeStatus Status, Guid? ChargeId);

public interface IPaymentGateway
{
    Task<PaymentChargeResult> Charge(
        string email,
        PaymentCard card,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentRefundStatus> Refund(
        Guid chargeId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
