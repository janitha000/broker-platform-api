using Audit.Application.Abstractions;
using Audit.Application.Auth;
using Audit.Application.Events.ListAuditEvents;
using Audit.Domain.Events;

namespace Audit.Application.Tests.Events;

public sealed class ListAuditEventsHandlerTests
{
    [Fact]
    public async Task Handle_FiltersToCurrentTenant_NewestFirst()
    {
        var tenant = Guid.NewGuid();
        var other = Guid.NewGuid();
        var store = new InMemoryAuditStore();
        await store.Seed(new AuditEventRecord
        {
            EventId = Guid.NewGuid(),
            TenantId = tenant,
            OccurredAt = DateTime.UtcNow.AddMinutes(-2),
            Action = "case.view",
            Outcome = "allow",
            ActorType = "user",
            ResourceType = "case",
            ResourceId = "a",
            RecordHash = "1",
        });
        await store.Seed(new AuditEventRecord
        {
            EventId = Guid.NewGuid(),
            TenantId = tenant,
            OccurredAt = DateTime.UtcNow.AddMinutes(-1),
            Action = "case.fact-find.complete",
            Outcome = "allow",
            ActorType = "user",
            ResourceType = "case",
            ResourceId = "b",
            RecordHash = "2",
        });
        await store.Seed(new AuditEventRecord
        {
            EventId = Guid.NewGuid(),
            TenantId = other,
            OccurredAt = DateTime.UtcNow,
            Action = "case.view",
            Outcome = "allow",
            ActorType = "user",
            ResourceType = "case",
            ResourceId = "c",
            RecordHash = "3",
        });

        var result = await new ListAuditEventsHandler(
                store,
                new StubCurrentBroker(Guid.NewGuid(), tenant, AuditPermissions.Read))
            .Handle(new ListAuditEventsQuery(null, null, null, null, null, 50));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("case.fact-find.complete", result.Items[0].Action);
    }

    [Fact]
    public async Task Handle_WithoutPermission_ReturnsEmpty()
    {
        var tenant = Guid.NewGuid();
        var store = new InMemoryAuditStore();
        await store.Seed(new AuditEventRecord
        {
            EventId = Guid.NewGuid(),
            TenantId = tenant,
            OccurredAt = DateTime.UtcNow,
            Action = "case.view",
            Outcome = "allow",
            ActorType = "user",
            ResourceType = "case",
            ResourceId = "a",
            RecordHash = "1",
        });

        var result = await new ListAuditEventsHandler(
                store,
                new StubCurrentBroker(Guid.NewGuid(), tenant))
            .Handle(new ListAuditEventsQuery(null, null, null, null, null, 50));

        Assert.Empty(result.Items);
    }
}

file sealed class StubCurrentBroker(Guid brokerId, Guid tenantId, params string[] permissions) : ICurrentBroker
{
    public Guid BrokerId { get; } = brokerId;
    public Guid TenantId { get; } = tenantId;

    public bool HasPermission(string permission) => permissions.Contains(permission);
}

file sealed class InMemoryAuditStore : IAuditStore
{
    private readonly List<AuditEventRecord> _rows = [];

    public Task Seed(AuditEventRecord record)
    {
        _rows.Add(record);
        return Task.CompletedTask;
    }

    public Task<AuditEventRecord?> GetByEventId(Guid eventId, CancellationToken ct) =>
        Task.FromResult(_rows.FirstOrDefault(r => r.EventId == eventId));

    public Task<AuditEventRecord> InsertChained(AuditEventRecord record, CancellationToken ct)
    {
        _rows.Add(record);
        return Task.FromResult(record);
    }

    public Task MarkArchived(Guid eventId, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<AuditEventRecord>> List(
        Guid tenantId,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? caseId,
        string? action,
        string? outcome,
        int take,
        CancellationToken ct)
    {
        var query = _rows.Where(e => e.TenantId == tenantId);
        if (fromUtc is DateTime from)
            query = query.Where(e => e.OccurredAt >= from);
        if (toUtc is DateTime to)
            query = query.Where(e => e.OccurredAt <= to);
        if (caseId is Guid id)
            query = query.Where(e => e.CaseId == id);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(e => e.Action == action);
        if (!string.IsNullOrEmpty(outcome))
            query = query.Where(e => e.Outcome == outcome);

        IReadOnlyList<AuditEventRecord> items = query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.EventId)
            .Take(take)
            .ToList();
        return Task.FromResult(items);
    }
}
