using Document.Domain.Documents;

namespace Document.Application.Documents.CreateDocumentUpload;

public sealed record CreateDocumentUploadResult(
    Guid DocumentId,
    DocumentStatus Status,
    string UploadUrl,
    string Token,
    string Method);