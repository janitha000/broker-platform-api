using Document.Application.Documents;

namespace Document.Application.Documents.CompleteDocumentUpload;

public enum CompleteDocumentUploadKind
{
    Succeeded,
    NotFound,
    Invalid,
    Conflict
}

public sealed record CompleteDocumentUploadOutcome(
    CompleteDocumentUploadKind Kind,
    CaseDocumentItem? Result,
    string? Error);