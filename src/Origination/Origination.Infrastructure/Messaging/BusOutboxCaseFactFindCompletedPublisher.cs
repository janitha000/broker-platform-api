using Broker.Contracts.Origination;
using MassTransit;
using Origination.Application.Abstractions;

namespace Origination.Infrastructure.Messaging;

public sealed class BusOutboxCaseFactFindCompletedPublisher : ICaseFactFindCompletedPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public BusOutboxCaseFactFindCompletedPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public bool UsesBusOutbox => true;

    public Task Publish(CaseFactFindCompleted message, CancellationToken cancellationToken = default) =>
        _publishEndpoint.Publish(message, cancellationToken);
}
