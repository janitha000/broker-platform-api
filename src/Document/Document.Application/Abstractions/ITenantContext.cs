namespace Document.Application.Abstractions;

public interface ITenantContext
{
    Guid? TenantId { get; }
}
