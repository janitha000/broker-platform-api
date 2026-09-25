using MassTransit;

namespace Origination.Infrastructure.Messaging.Sagas;

public sealed class CaseLifecycleState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public Guid BrokerId { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? FactFindCompletedAt { get; set; }
}
