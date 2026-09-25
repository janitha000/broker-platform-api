using Broker.Contracts.Origination;

namespace Origination.Application.Abstractions;

public interface ICaseFactFindCompletedPublisher
{
    bool UsesBusOutbox { get; }

    Task Publish(CaseFactFindCompleted message, CancellationToken cancellationToken = default);
}
