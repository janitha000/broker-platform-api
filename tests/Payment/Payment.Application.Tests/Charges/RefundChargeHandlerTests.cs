using Payment.Application.Abstractions;
using Payment.Application.Charges.CreateCharge;
using Payment.Application.Charges.RefundCharge;
using Payment.Domain.Charges;

namespace Payment.Application.Tests.Charges;

public sealed class RefundChargeHandlerTests
{
    [Fact]
    public async Task Handle_UnknownCharge_IsNotFound()
    {
        var handler = new RefundChargeHandler(new InMemoryChargeRepository());

        var outcome = await handler.Handle(new RefundChargeCommand(Guid.NewGuid(), "refund-1"));

        Assert.Equal(RefundChargeKind.NotFound, outcome.Kind);
        Assert.Null(outcome.ChargeId);
    }

    [Fact]
    public async Task Handle_SucceededCharge_MarksRefunded()
    {
        var repo = new InMemoryChargeRepository();
        var created = await new CreateChargeHandler(repo, new StubCardGateway(true))
            .Handle(new CreateChargeCommand(
                "broker@example.com",
                new CardDetails("4242424242424242", 12, 2030, "123"),
                "key-1"));
        var handler = new RefundChargeHandler(repo);

        var outcome = await handler.Handle(
            new RefundChargeCommand(created.Charge!.ChargeId, "refund-1"));

        Assert.Equal(RefundChargeKind.Succeeded, outcome.Kind);
        var stored = await repo.GetById(created.Charge.ChargeId);
        Assert.Equal(ChargeStatus.Refunded, stored!.Status);
        Assert.NotNull(stored.RefundedAt);
    }

    [Fact]
    public async Task Handle_AlreadyRefunded_IsSucceededIdempotent()
    {
        var repo = new InMemoryChargeRepository();
        var created = await new CreateChargeHandler(repo, new StubCardGateway(true))
            .Handle(new CreateChargeCommand(
                "broker@example.com",
                new CardDetails("4242424242424242", 12, 2030, "123"),
                "key-1"));
        var handler = new RefundChargeHandler(repo);
        var command = new RefundChargeCommand(created.Charge!.ChargeId, "refund-1");
        await handler.Handle(command);

        var second = await handler.Handle(command);

        Assert.Equal(RefundChargeKind.Succeeded, second.Kind);
        Assert.Equal(created.Charge.ChargeId, second.ChargeId);
    }

    [Fact]
    public async Task Handle_DeclinedCharge_IsNotRefundable()
    {
        var repo = new InMemoryChargeRepository();
        var created = await new CreateChargeHandler(repo, new StubCardGateway(false))
            .Handle(new CreateChargeCommand(
                "broker@example.com",
                new CardDetails("4000000000000002", 12, 2030, "123"),
                "key-1"));
        var handler = new RefundChargeHandler(repo);

        var outcome = await handler.Handle(
            new RefundChargeCommand(created.Charge!.ChargeId, "refund-1"));

        Assert.Equal(RefundChargeKind.NotRefundable, outcome.Kind);
    }
}

file sealed class StubCardGateway(bool authorised) : ICardGateway
{
    public Task<bool> Charge(string cardNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(authorised);
}

file sealed class InMemoryChargeRepository : IChargeRepository
{
    private readonly Dictionary<string, Charge> _byKey = new();

    public Task<Charge?> GetByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        _byKey.TryGetValue(idempotencyKey, out var charge);
        return Task.FromResult(charge);
    }

    public Task<Charge?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var charge = _byKey.Values.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(charge);
    }

    public Task<Charge> Add(Charge charge, CancellationToken cancellationToken = default)
    {
        _byKey.Add(charge.IdempotencyKey, charge);
        return Task.FromResult(charge);
    }

    public Task Update(Charge charge, CancellationToken cancellationToken = default)
    {
        _byKey[charge.IdempotencyKey] = charge;
        return Task.CompletedTask;
    }
}
