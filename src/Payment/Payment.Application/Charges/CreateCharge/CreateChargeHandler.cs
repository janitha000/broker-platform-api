using Payment.Application.Abstractions;
using Payment.Domain.Charges;

namespace Payment.Application.Charges.CreateCharge;

public sealed class CreateChargeHandler
{
    private readonly IChargeRepository _charges;
    private readonly ICardGateway _cardGateway;

    public CreateChargeHandler(IChargeRepository charges, ICardGateway cardGateway)
    {
        _charges = charges;
        _cardGateway = cardGateway;
    }

    public async Task<CreateChargeOutcome> Handle(
        CreateChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        var key = command.IdempotencyKey.Trim();
        var email = command.Email.Trim().ToLowerInvariant();
        var fingerprint = Fingerprint(email, command.Card);

        var existing = await _charges.GetByIdempotencyKey(key, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.PayloadFingerprint, fingerprint, StringComparison.Ordinal))
                return new CreateChargeOutcome(CreateChargeKind.IdempotencyConflict, null);

            return ToOutcome(existing);
        }

        var authorised = await _cardGateway.Charge(command.Card.Number, cancellationToken);
        var charge = new Charge
        {
            Id = Guid.NewGuid(),
            Email = email,
            Status = authorised ? ChargeStatus.Succeeded : ChargeStatus.Declined,
            IdempotencyKey = key,
            PayloadFingerprint = fingerprint,
            CreatedAt = DateTime.UtcNow,
        };
        charge = await _charges.Add(charge, cancellationToken);
        return ToOutcome(charge);
    }

    private static CreateChargeOutcome ToOutcome(Charge charge)
    {
        var result = new CreateChargeResult(charge.Id, charge.Status);
        var kind = charge.Status == ChargeStatus.Declined
            ? CreateChargeKind.Declined
            : CreateChargeKind.Succeeded;
        return new CreateChargeOutcome(kind, result);
    }

    private static string Fingerprint(string email, CardDetails card)
    {
        var number = new string((card.Number ?? string.Empty).Where(char.IsDigit).ToArray());
        var cvc = (card.Cvc ?? string.Empty).Trim();
        return $"{email}|{number}|{card.ExpMonth}|{card.ExpYear}|{cvc}";
    }
}