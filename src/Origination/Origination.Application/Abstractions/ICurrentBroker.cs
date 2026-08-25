namespace Origination.Application.Abstractions;

public interface ICurrentBroker
{
    Guid BrokerId { get; }
    Guid TenantId { get; }
}