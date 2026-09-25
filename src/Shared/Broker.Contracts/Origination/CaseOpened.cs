namespace Broker.Contracts.Origination;

/// <summary>
/// Published when a case is created. The Origination case-lifecycle saga
/// starts from this event (correlation id = <see cref="CaseId"/>).
/// </summary>
public sealed record CaseOpened
{
    public Guid CaseId { get; init; }
    public Guid TenantId { get; init; }
    public Guid BrokerId { get; init; }
    public DateTime OccurredAt { get; init; }
}
