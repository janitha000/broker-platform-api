using Broker.Contracts.Origination;
using Origination.Application.Abstractions;

namespace Origination.Infrastructure.Messaging;

public sealed class DisabledBusOutboxCaseOpenedPublisher : ICaseOpenedPublisher
{
    public bool UsesBusOutbox => false;

    public Task Publish(CaseOpened message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
