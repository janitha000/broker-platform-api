namespace Document.Application.Abstractions;

public interface ICurrentBroker
{
    Guid BrokerId { get; }
    Guid TenantId { get; }
    bool HasPermission(string permission);
}
