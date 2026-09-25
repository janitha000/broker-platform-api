using Broker.Contracts;
using Broker.Contracts.Origination;
using Broker.Contracts.Notification;
using MassTransit;

namespace Origination.Infrastructure.Messaging.Sagas;

public sealed class CaseLifecycleStateMachine : MassTransitStateMachine<CaseLifecycleState>
{
    public static readonly Uri SendCaseFactFindEmailAddress =
        new($"queue:{BrokerCommandQueues.SendCaseFactFindEmail}");

    public CaseLifecycleStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => CaseOpened, x => x.CorrelateById(context => context.Message.CaseId));

        Event(() => CaseFactFindCompleted, x =>
        {
            x.CorrelateById(context => context.Message.CaseId);
            x.OnMissingInstance(m => m.Discard());
        });

        Initially(
            When(CaseOpened)
                .Then(context =>
                {
                    context.Saga.TenantId = context.Message.TenantId;
                    context.Saga.BrokerId = context.Message.BrokerId;
                    context.Saga.OpenedAt = context.Message.OccurredAt;
                })
                .TransitionTo(Enquiry));

        During(Enquiry,
            Ignore(CaseOpened),
            When(CaseFactFindCompleted)
                .Then(context => context.Saga.FactFindCompletedAt = DateTime.UtcNow)
                .Send(SendCaseFactFindEmailAddress, context => new SendCaseFactFindEmail
                {
                    CaseId = context.Message.CaseId,
                    TenantId = context.Message.TenantId,
                    BrokerId = context.Message.BrokerId,
                    TemplateKey = context.Message.TemplateKey,
                    Channel = context.Message.Channel,
                    Data = context.Message.Data,
                    IdempotencyKey = context.Message.IdempotencyKey,
                    CorrelationId = context.Message.CorrelationId,
                    Recipient = context.Message.Recipient,
                })
                .TransitionTo(FactFindCompleted));

        During(FactFindCompleted,
            Ignore(CaseOpened),
            Ignore(CaseFactFindCompleted));
    }

    public State Enquiry { get; private set; } = null!;

    public State FactFindCompleted { get; private set; } = null!;

    public Event<CaseOpened> CaseOpened { get; private set; } = null!;

    public Event<CaseFactFindCompleted> CaseFactFindCompleted { get; private set; } = null!;
}
