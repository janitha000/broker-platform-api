namespace Origination.Application.Abstractions;

public interface IMessageBus
{
    Task Publish(string type, string payload, CancellationToken cancellationToken = default);
}