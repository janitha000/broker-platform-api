namespace Identity.Application.Abstractions;

public enum PaymentChargeStatus
{
    Succeeded,
    Declined,
    Conflict,
    Unavailable,
}

public sealed record PaymentCard(
    string Number,
    int ExpMonth,
    int ExpYear,
    string Cvc);

public interface IPaymentGateway
{
    Task<PaymentChargeStatus> Charge(
        string email,
        PaymentCard card,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}