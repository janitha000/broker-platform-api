using Payment.Application.Abstractions;
using Payment.Application.Charges.CreateCharge;
using Payment.Domain.Charges;

namespace Payment.Application.Tests.Charges;

public sealed class CreateChargeHandlerTests
{
    private static CreateChargeCommand Command(
        string key = "key-1",
        string number = "4242424242424242") =>
        new("broker@example.com", new CardDetails(number, 12, 2030, "123"), key);

    [Fact]
    public async Task Handle_AuthorisedCard_PersistsSucceeded()
    {
        var repo = new InMemoryChargeRepository();
        var handler = new CreateChargeHandler(repo, new StubCardGateway(authorised: true));

        var outcome = await handler.Handle(Command());

        Assert.Equal(CreateChargeKind.Succeeded, outcome.Kind);
        Assert.Equal(ChargeStatus.Succeeded, outcome.Charge!.Status);
        var stored = await repo.GetByIdempotencyKey("key-1");
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task Handle_DeclinedCard_PersistsDeclined()
    {
        var repo = new InMemoryChargeRepository();
        var handler = new CreateChargeHandler(repo, new StubCardGateway(authorised: false));

        var outcome = await handler.Handle(Command(number: "4000000000000002"));

        Assert.Equal(CreateChargeKind.Declined, outcome.Kind);
        Assert.Equal(ChargeStatus.Declined, outcome.Charge!.Status);
    }

    [Fact]
    public async Task Handle_SameKeySamePayload_ReplaysWithoutSecondCharge()
    {
        var repo = new InMemoryChargeRepository();
        var gateway = new CountingGateway(authorised: true);
        var handler = new CreateChargeHandler(repo, gateway);

        var first = await handler.Handle(Command());
        var second = await handler.Handle(Command());

        Assert.Equal(first.Charge!.ChargeId, second.Charge!.ChargeId);
        Assert.Equal(1, gateway.Calls);
    }

    [Fact]
    public async Task Handle_SameKeyDifferentCard_IsConflict()
    {
        var repo = new InMemoryChargeRepository();
        var handler = new CreateChargeHandler(repo, new StubCardGateway(true));

        await handler.Handle(Command(number: "4242424242424242"));
        var second = await handler.Handle(Command(number: "5555555555554444"));

        Assert.Equal(CreateChargeKind.IdempotencyConflict, second.Kind);
    }

    [Fact]
    public async Task Handle_SameKeySpacedCardDigits_IsReplay()
    {
        var repo = new InMemoryChargeRepository();
        var handler = new CreateChargeHandler(repo, new StubCardGateway(true));

        await handler.Handle(Command(number: "4242424242424242"));
        var second = await handler.Handle(Command(number: "4242 4242 4242 4242"));

        Assert.Equal(CreateChargeKind.Succeeded, second.Kind);
    }
}

file sealed class StubCardGateway(bool authorised) : ICardGateway
{
    public Task<bool> Charge(string cardNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(authorised);
}

file sealed class CountingGateway(bool authorised) : ICardGateway
{
    public int Calls { get; private set; }

    public Task<bool> Charge(string cardNumber, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(authorised);
    }
}

file sealed class InMemoryChargeRepository : IChargeRepository
{
    private readonly Dictionary<string, Charge> _byKey = new();

    public Task<Charge?> GetByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        _byKey.TryGetValue(idempotencyKey, out var charge);
        return Task.FromResult(charge);
    }

    public Task<Charge> Add(Charge charge, CancellationToken cancellationToken = default)
    {
        _byKey.Add(charge.IdempotencyKey, charge);
        return Task.FromResult(charge);
    }
}
