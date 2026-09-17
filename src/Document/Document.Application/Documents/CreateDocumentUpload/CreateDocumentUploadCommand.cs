using Document.Domain.Documents;

namespace Document.Application.Documents.CreateDocumentUpload;

public sealed record CreateDocumentUploadCommand(
    Guid CaseId,
    DocumentType Type,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string IdempotencyKey);