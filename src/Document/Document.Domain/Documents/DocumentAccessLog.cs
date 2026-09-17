using Document.Domain.Abstractions;

namespace Document.Domain.Documents;

public sealed class DocumentAccessLog : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? BrokerId { get; set; }
    public DocumentAccessAction Action { get; set; }
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; }
}