using Payment.Application.Abstractions;

namespace Payment.Infrastructure.Payments;

public sealed class MockCardGateway : ICardGateway
{
    public const string DeclineNumber = "4000000000000002";

    public Task<bool> Charge(string cardNumber, CancellationToken cancellationToken = default)
    {
        var digits = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        var authorised = digits != DeclineNumber;
        return Task.FromResult(authorised);
    }
}