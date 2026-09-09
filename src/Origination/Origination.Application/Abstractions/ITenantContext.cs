namespace Origination.Application.Abstractions;

public interface ITenantContext
{
    Guid? TenantId { get; }
}