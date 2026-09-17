namespace Document.Domain.Abstractions;

public interface ITenantOwnedEntity
{
    Guid TenantId { get; set; }
}
