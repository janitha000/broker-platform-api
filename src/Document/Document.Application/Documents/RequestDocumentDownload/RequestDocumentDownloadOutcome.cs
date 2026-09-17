namespace Document.Application.Documents.RequestDocumentDownload;

public enum RequestDocumentDownloadKind
{
    Succeeded,
    NotFound,
    Forbidden,
    Conflict
}

public sealed record RequestDocumentDownloadOutcome(
    RequestDocumentDownloadKind Kind,
    RequestDocumentDownloadResult? Result,
    string? Error);