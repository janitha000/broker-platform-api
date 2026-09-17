namespace Document.Application.Documents.CreateDocumentUpload;

public enum CreateDocumentUploadKind
{
    Succeeded,
    Invalid
}

public sealed record CreateDocumentUploadOutcome(
    CreateDocumentUploadKind Kind,
    CreateDocumentUploadResult? Result,
    string? Error);