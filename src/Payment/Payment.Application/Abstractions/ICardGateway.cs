namespace Payment.Application.Abstractions;

public interface ICardGateway
{
    Task<bool> Charge(string cardNumber, CancellationToken cancellationToken = default);
}