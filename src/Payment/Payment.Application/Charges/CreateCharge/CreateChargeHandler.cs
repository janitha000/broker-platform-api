namespace Payment.Application.Charges.CreateCharge;

public sealed class CreateChargeHandler
{
    public Task<CreateChargeResult> Handle(
        CreateChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
