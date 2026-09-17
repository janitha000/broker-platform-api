using Document.Application.Auth;
using Document.Domain.Documents;

namespace Document.Application.Documents;

public sealed record CaseDocumentItem(
    Guid DocumentId,
    DocumentType Type,
    DocumentStatus Status,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DocumentSensitivity Sensitivity,
    bool CanDownload,
    DateTime CreatedAt);

public static class DocumentAccess
{
    public static bool CanDownload(CaseDocument document, bool canReadSensitive) =>
        document.Status == DocumentStatus.Clean
        && (document.Sensitivity != DocumentSensitivity.Sensitive || canReadSensitive);

    public static CaseDocumentItem ToItem(CaseDocument document, bool canReadSensitive) =>
        new(
            document.Id,
            document.Type,
            document.Status,
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            document.Sensitivity,
            CanDownload(document, canReadSensitive),
            document.CreatedAt);

    public static DocumentAccessLog Log(
        CaseDocument document,
        Guid? brokerId,
        DocumentAccessAction action,
        string? detail = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = document.TenantId,
            DocumentId = document.Id,
            BrokerId = brokerId,
            Action = action,
            Detail = detail,
            OccurredAt = DateTime.UtcNow,
        };
}