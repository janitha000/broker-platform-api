using Audit.Application.Abstractions;
using Audit.Application.Auth;

namespace Audit.Application.Events.ListAuditEvents;

public sealed class ListAuditEventsHandler
{
    public const int DefaultTake = 50;
    public const int MaxTake = 200;

    private readonly IAuditStore _store;
    private readonly ICurrentBroker _currentBroker;

    public ListAuditEventsHandler(IAuditStore store, ICurrentBroker currentBroker)
    {
        _store = store;
        _currentBroker = currentBroker;
    }

    public async Task<ListAuditEventsResult> Handle(
        ListAuditEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentBroker.HasPermission(AuditPermissions.Read))
            return new ListAuditEventsResult([]);

        var take = query.Take <= 0 ? DefaultTake : Math.Min(query.Take, MaxTake);
        var rows = await _store.List(
            _currentBroker.TenantId,
            query.FromUtc,
            query.ToUtc,
            query.CaseId,
            string.IsNullOrWhiteSpace(query.Action) ? null : query.Action.Trim(),
            string.IsNullOrWhiteSpace(query.Outcome) ? null : query.Outcome.Trim(),
            take,
            cancellationToken);

        return new ListAuditEventsResult(rows.Select(ToItem).ToList());
    }

    private static AuditEventItem ToItem(Domain.Events.AuditEventRecord row) =>
        new(
            row.EventId,
            row.OccurredAt,
            row.Action,
            row.Outcome,
            row.ActorType,
            row.BrokerId,
            row.ResourceType,
            row.ResourceId,
            row.CaseId,
            row.Sensitivity,
            row.CorrelationId,
            row.TraceId,
            row.DataJson,
            row.RecordHash);
}
