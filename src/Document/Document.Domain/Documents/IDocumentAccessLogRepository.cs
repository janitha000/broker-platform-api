namespace Document.Domain.Documents;

public interface IDocumentAccessLogRepository
{
    Task Add(DocumentAccessLog log, CancellationToken cancellationToken = default);
}