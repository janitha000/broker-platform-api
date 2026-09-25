using Broker.Contracts.Origination;
using Origination.Application.Abstractions;

namespace Origination.Infrastructure.Messaging;

public sealed class DisabledBusOutboxCaseFactFindCompletedPublisher : ICaseFactFindCompletedPublisher
{
    public bool UsesBusOutbox => false;

    public Task Publish(CaseFactFindCompleted message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
