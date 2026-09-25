using Broker.Contracts.Origination;

namespace Origination.Application.Abstractions;

public interface ICaseOpenedPublisher
{
    bool UsesBusOutbox { get; }

    Task Publish(CaseOpened message, CancellationToken cancellationToken = default);
}
