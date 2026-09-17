using Document.Domain.Abstractions;

namespace Document.Domain.Documents;

public sealed class CaseDocument : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid UploadedByBrokerId { get; set; }

    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }
    public DocumentSensitivity Sensitivity { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;

    public string LandingKey { get; set; } = string.Empty;
    public string? CleanKey { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DateTime? ScannedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public static DocumentSensitivity SensitivityFor(DocumentType type) =>
        type is DocumentType.PhotoId or DocumentType.BankStatement
            ? DocumentSensitivity.Sensitive
            : DocumentSensitivity.Standard;
}