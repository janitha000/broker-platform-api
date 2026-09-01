using Origination.Application.Abstractions;
using Origination.Application.Cases.CompleteFactFind;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;
using Origination.Domain.Outbox;

namespace Origination.Application.Tests.Cases;

public sealed class CompleteFactFindHandlerTests
{
    [Fact]
    public async Task Handle_ExistingCase_SetsFactFindAndEnqueuesOutbox()
    {
        var caseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new InMemoryFactFindCaseRepository();
        var outbox = new InMemoryOutbox();
        await repository.Add(new Case
        {
            Id = caseId,
            TenantId = tenantId,
            BrokerId = Guid.NewGuid(),
            Status = CaseStatus.Inquiry,
            CreatedAt = DateTime.UtcNow
        });

        var handler = new CompleteFactFindHandler(
            repository,
            new StubCurrentBroker(Guid.NewGuid(), tenantId),
            outbox,
            new InMemoryUnitOfWork());
        var result = await handler.Handle(new CompleteFactFindCommand(
            caseId,
            "Buy first home",
            120_000m,
            40_000m,
            80_000m,
            15_000m));

        Assert.NotNull(result);
        Assert.Equal(CaseStatus.FactFindCompleted, result!.Status);

        var stored = await repository.GetById(caseId, tenantId);
        Assert.NotNull(stored!.FactFind);
        Assert.Equal("Buy first home", stored.FactFind.Objectives);
        Assert.Equal(120_000m, stored.FactFind.Income);
        Assert.Equal(40_000m, stored.FactFind.Expenses);
        Assert.Equal(80_000m, stored.FactFind.Assets);
        Assert.Equal(15_000m, stored.FactFind.Debts);

        var message = Assert.Single(outbox.Messages);
        Assert.Equal(OutboxMessageTypes.CaseFactFindCompleted, message.Type);
        Assert.Equal($"origination:{caseId}:fact-find-completed:email", message.IdempotencyKey);
        Assert.Null(message.PublishedAt);
    }

    [Fact]
    public async Task Handle_SameCaseTwice_DoesNotEnqueueSecondOutboxRow()
    {
        var caseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new InMemoryFactFindCaseRepository();
        var outbox = new InMemoryOutbox();
        await repository.Add(new Case
        {
            Id = caseId,
            TenantId = tenantId,
            BrokerId = Guid.NewGuid(),
            Status = CaseStatus.Inquiry,
            CreatedAt = DateTime.UtcNow
        });

        var handler = new CompleteFactFindHandler(
            repository,
            new StubCurrentBroker(Guid.NewGuid(), tenantId),
            outbox,
            new InMemoryUnitOfWork());
        var command = new CompleteFactFindCommand(caseId, "Buy first home", 1m, 1m, 1m, 1m);

        await handler.Handle(command);
        await handler.Handle(command);

        Assert.Single(outbox.Messages);
    }

    [Fact]
    public async Task Handle_MissingCase_ReturnsNull()
    {
        var handler = new CompleteFactFindHandler(
            new InMemoryFactFindCaseRepository(),
            new StubCurrentBroker(Guid.NewGuid(), Guid.NewGuid()),
            new InMemoryOutbox(),
            new InMemoryUnitOfWork());

        var result = await handler.Handle(new CompleteFactFindCommand(
            Guid.NewGuid(),
            "x",
            1m, 1m, 1m, 1m));

        Assert.Null(result);
    }
}

file sealed class StubCurrentBroker(Guid brokerId, Guid tenantId) : ICurrentBroker
{
    public Guid BrokerId { get; } = brokerId;
    public Guid TenantId { get; } = tenantId;
}

file sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task SaveChanges(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

file sealed class InMemoryOutbox : IOutbox
{
    public List<OutboxMessage> Messages { get; } = [];

    public Task<bool> Exists(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Messages.Any(m => m.IdempotencyKey == idempotencyKey));

    public void Add(OutboxMessage message) => Messages.Add(message);
}

file sealed class InMemoryFactFindCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();

    public Task Add(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }

    public Task Update(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }

    public Task<Case?> GetById(Guid caseId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        _cases.TryGetValue(caseId, out var @case);
        if (@case is null || @case.TenantId != tenantId)
            return Task.FromResult<Case?>(null);
        return Task.FromResult<Case?>(@case);
    }

    public Task<IEnumerable<Case>> GetCasesByTenantId(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var matches = _cases.Values
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .AsEnumerable();
        return Task.FromResult(matches);
    }
}
