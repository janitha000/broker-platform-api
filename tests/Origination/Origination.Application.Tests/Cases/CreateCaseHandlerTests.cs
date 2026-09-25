using Origination.Application.Abstractions;
using Origination.Application.Cases.CreateCase;
using Origination.Application.Cases.GetCase;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;
using Broker.Hosting.Audit;

namespace Origination.Application.Tests.Cases;

public sealed class CreateCaseHandlerTests
{
    [Fact]
    public async Task Handle_NewCase_HasEnquiryStatusAndTenant()
    {
        var repository = new InMemoryCaseRepository();
        var brokerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var handler = new CreateCaseHandler(
            repository,
            new StubCurrentBroker(brokerId, tenantId),
            new InMemoryUnitOfWork(),
            new DisabledCaseOpenedPublisher());

        var result = await handler.Handle(new CreateCaseCommand("First home inquiry"));

        Assert.Equal(CaseStatus.Enquiry, result.Status);
        Assert.NotEqual(Guid.Empty, result.CaseId);

        var stored = await repository.GetById(result.CaseId, tenantId);
        Assert.NotNull(stored);
        Assert.Equal(brokerId, stored!.BrokerId);
        Assert.Equal(tenantId, stored.TenantId);
        Assert.Equal("First home inquiry", stored.InquiryNotes);
    }

    [Fact]
    public async Task Handle_BusOutbox_PublishesCaseOpened()
    {
        var repository = new InMemoryCaseRepository();
        var brokerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var bus = new RecordingCaseOpenedPublisher();
        var handler = new CreateCaseHandler(
            repository,
            new StubCurrentBroker(brokerId, tenantId),
            new InMemoryUnitOfWork(),
            bus);

        var result = await handler.Handle(new CreateCaseCommand("notes"));

        var published = Assert.Single(bus.Published);
        Assert.Equal(result.CaseId, published.CaseId);
        Assert.Equal(tenantId, published.TenantId);
        Assert.Equal(brokerId, published.BrokerId);
    }
}

public sealed class GetCaseHandlerTests
{
    [Fact]
    public async Task Handle_OtherTenant_ReturnsNull()
    {
        var repository = new InMemoryCaseRepository();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var created = await new CreateCaseHandler(
                repository,
                new StubCurrentBroker(Guid.NewGuid(), tenantA),
                new InMemoryUnitOfWork(),
                new DisabledCaseOpenedPublisher())
            .Handle(new CreateCaseCommand("notes"));

        var result = await new GetCaseHandler(
                repository,
                new StubCurrentBroker(Guid.NewGuid(), tenantB),
                new InMemoryUnitOfWork(),
                new NoopAuditRecorder())
            .Handle(new GetCaseQuery(created.CaseId));

        Assert.Null(result);
    }
}

file sealed class DisabledCaseOpenedPublisher : ICaseOpenedPublisher
{
    public bool UsesBusOutbox => false;

    public Task Publish(
        Broker.Contracts.Origination.CaseOpened message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class RecordingCaseOpenedPublisher : ICaseOpenedPublisher
{
    public bool UsesBusOutbox => true;

    public List<Broker.Contracts.Origination.CaseOpened> Published { get; } = [];

    public Task Publish(
        Broker.Contracts.Origination.CaseOpened message,
        CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
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

file sealed class InMemoryCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();

    public Task Add(Case @case, CancellationToken cancellationToken = default)
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

    public Task Update(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }
}
