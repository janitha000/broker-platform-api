using Origination.Application.Abstractions;
using Origination.Application.Auth;
using Origination.Application.Cases.CompleteFactFind;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;
using Origination.Domain.Outbox;
using Broker.Hosting.Audit;

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
            Status = CaseStatus.Enquiry,
            CreatedAt = DateTime.UtcNow
        });

        var handler = new CompleteFactFindHandler(
            repository,
            new StubCurrentBroker(Guid.NewGuid(), tenantId, CasePermissions.FactFindAny),
            outbox,
            new InMemoryUnitOfWork(),
            new NoopAuditRecorder(),
            new JsonOutboxFactFindPublisher());
        var outcome = await handler.Handle(new CompleteFactFindCommand(
            caseId,
            "Buy first home",
            120_000m,
            40_000m,
            80_000m,
            15_000m));

        Assert.Equal(CompleteFactFindKind.Succeeded, outcome.Kind);
        Assert.Equal(CaseStatus.FactFindCompleted, outcome.Result!.Status);

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
    public async Task Handle_BusOutbox_PublishesTypedMessageAndMarksJsonOutboxDelivered()
    {
        var caseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brokerId = Guid.NewGuid();
        var repository = new InMemoryFactFindCaseRepository();
        var outbox = new InMemoryOutbox();
        var bus = new RecordingBusOutboxPublisher();
        await repository.Add(new Case
        {
            Id = caseId,
            TenantId = tenantId,
            BrokerId = brokerId,
            Status = CaseStatus.Enquiry,
            CreatedAt = DateTime.UtcNow,
        });

        var outcome = await new CompleteFactFindHandler(
            repository,
            new StubCurrentBroker(brokerId, tenantId, CasePermissions.FactFindAny),
            outbox,
            new InMemoryUnitOfWork(),
            new NoopAuditRecorder(),
            bus)
            .Handle(new CompleteFactFindCommand(caseId, "Buy first home", 1m, 1m, 1m, 1m));

        Assert.Equal(CompleteFactFindKind.Succeeded, outcome.Kind);
        var published = Assert.Single(bus.Published);
        Assert.Equal(caseId, published.CaseId);
        Assert.Equal($"origination:{caseId}:fact-find-completed:email", published.IdempotencyKey);
        var marker = Assert.Single(outbox.Messages);
        Assert.NotNull(marker.PublishedAt);
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
            Status = CaseStatus.Enquiry,
            CreatedAt = DateTime.UtcNow
        });

        var handler = new CompleteFactFindHandler(
            repository,
            new StubCurrentBroker(Guid.NewGuid(), tenantId, CasePermissions.FactFindAny),
            outbox,
            new InMemoryUnitOfWork(),
            new NoopAuditRecorder(),
            new JsonOutboxFactFindPublisher());
        var command = new CompleteFactFindCommand(caseId, "Buy first home", 1m, 1m, 1m, 1m);

        await handler.Handle(command);
        await handler.Handle(command);

        Assert.Single(outbox.Messages);
    }

    [Fact]
    public async Task Handle_MissingCase_ReturnsNotFound()
    {
        var handler = new CompleteFactFindHandler(
            new InMemoryFactFindCaseRepository(),
            new StubCurrentBroker(Guid.NewGuid(), Guid.NewGuid(), CasePermissions.FactFindAny),
            new InMemoryOutbox(),
            new InMemoryUnitOfWork(),
            new NoopAuditRecorder(),
            new JsonOutboxFactFindPublisher());

        var outcome = await handler.Handle(new CompleteFactFindCommand(
            Guid.NewGuid(),
            "x",
            1m, 1m, 1m, 1m));

        Assert.Equal(CompleteFactFindKind.NotFound, outcome.Kind);
    }

    [Fact]
    public async Task Handle_Assistant_OwnCase_Succeeds()
    {
        var caseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assistantId = Guid.NewGuid();
        var repository = new InMemoryFactFindCaseRepository();
        await repository.Add(new Case
        {
            Id = caseId,
            TenantId = tenantId,
            BrokerId = assistantId,
            Status = CaseStatus.Enquiry,
            CreatedAt = DateTime.UtcNow,
        });

        var outcome = await new CompleteFactFindHandler(
            repository,
            new StubCurrentBroker(assistantId, tenantId, CasePermissions.FactFind),
            new InMemoryOutbox(),
            new InMemoryUnitOfWork(),
            new NoopAuditRecorder(),
            new JsonOutboxFactFindPublisher())
            .Handle(new CompleteFactFindCommand(caseId, "x", 1m, 1m, 1m, 1m));

        Assert.Equal(CompleteFactFindKind.Succeeded, outcome.Kind);
    }

    [Fact]
    public async Task Handle_Assistant_OtherBrokerCase_Forbidden()
    {
        var caseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new InMemoryFactFindCaseRepository();
        await repository.Add(new Case
        {
            Id = caseId,
            TenantId = tenantId,
            BrokerId = Guid.NewGuid(),
            Status = CaseStatus.Enquiry,
            CreatedAt = DateTime.UtcNow,
        });

        var outcome = await new CompleteFactFindHandler(
            repository,
            new StubCurrentBroker(Guid.NewGuid(), tenantId, CasePermissions.FactFind),
            new InMemoryOutbox(),
            new InMemoryUnitOfWork(),
            new NoopAuditRecorder(),
            new JsonOutboxFactFindPublisher())
            .Handle(new CompleteFactFindCommand(caseId, "x", 1m, 1m, 1m, 1m));

        Assert.Equal(CompleteFactFindKind.Forbidden, outcome.Kind);
        var stored = await repository.GetById(caseId, tenantId);
        Assert.Equal(CaseStatus.Enquiry, stored!.Status);
        Assert.Null(stored.FactFind);
    }
}

file sealed class StubCurrentBroker(
    Guid brokerId,
    Guid tenantId,
    params string[] permissions) : ICurrentBroker
{
    public Guid BrokerId { get; } = brokerId;
    public Guid TenantId { get; } = tenantId;

    public bool HasPermission(string permission) =>
        permissions.Contains(permission);
}

file sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task SaveChanges(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

file sealed class NoopAuditRecorder : IAuditRecorder
{
    public void Record(AuditEvent auditEvent) { }

    public Task Flush(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

file sealed class JsonOutboxFactFindPublisher : ICaseFactFindCompletedPublisher
{
    public bool UsesBusOutbox => false;

    public Task Publish(
        Broker.Contracts.Origination.CaseFactFindCompleted message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class RecordingBusOutboxPublisher : ICaseFactFindCompletedPublisher
{
    public bool UsesBusOutbox => true;

    public List<Broker.Contracts.Origination.CaseFactFindCompleted> Published { get; } = [];

    public Task Publish(
        Broker.Contracts.Origination.CaseFactFindCompleted message,
        CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
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
