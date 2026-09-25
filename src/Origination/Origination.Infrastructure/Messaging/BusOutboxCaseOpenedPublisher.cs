using Broker.Contracts.Origination;
using MassTransit;
using Origination.Application.Abstractions;

namespace Origination.Infrastructure.Messaging;

public sealed class BusOutboxCaseOpenedPublisher : ICaseOpenedPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public BusOutboxCaseOpenedPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public bool UsesBusOutbox => true;

    public Task Publish(CaseOpened message, CancellationToken cancellationToken = default) =>
        _publishEndpoint.Publish(message, cancellationToken);
}
