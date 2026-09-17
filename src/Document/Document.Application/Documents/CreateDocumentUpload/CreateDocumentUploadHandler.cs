using Document.Application.Abstractions;
using Document.Domain.Abstractions;
using Document.Domain.Documents;

namespace Document.Application.Documents.CreateDocumentUpload;

public sealed class CreateDocumentUploadHandler
{
    private static readonly TimeSpan UploadTtl = TimeSpan.FromMinutes(10);

    private readonly ICaseDocumentRepository _documents;
    private readonly IDocumentAccessLogRepository _accessLogs;
    private readonly IObjectStore _objectStore;
    private readonly ICurrentBroker _currentBroker;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentUploadHandler(
        ICaseDocumentRepository documents,
        IDocumentAccessLogRepository accessLogs,
        IObjectStore objectStore,
        ICurrentBroker currentBroker,
        IUnitOfWork unitOfWork)
    {
        _documents = documents;
        _accessLogs = accessLogs;
        _objectStore = objectStore;
        _currentBroker = currentBroker;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateDocumentUploadOutcome> Handle(
        CreateDocumentUploadCommand command,
        CancellationToken cancellationToken = default)
    {
        var error = Validate(command);
        if (error is not null)
            return new CreateDocumentUploadOutcome(CreateDocumentUploadKind.Invalid, null, error);

        var fileName = Path.GetFileName(command.FileName).Trim();
        var sha256 = command.Sha256.Trim().ToLowerInvariant();
        var idempotencyKey = command.IdempotencyKey.Trim();

        var existing = await _documents.GetByIdempotencyKey(
            _currentBroker.TenantId,
            idempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            if (!SameRequest(existing, command, fileName, sha256))
            {
                return new CreateDocumentUploadOutcome(
                    CreateDocumentUploadKind.Invalid,
                    null,
                    "Idempotency key was already used for a different upload.");
            }

            var replayGrant = existing.Status == DocumentStatus.PendingUpload
                ? _objectStore.CreateUploadGrant(existing.Id, existing.TenantId, UploadTtl)
                : new BlobGrant(string.Empty, string.Empty, "PUT");

            return new CreateDocumentUploadOutcome(
                CreateDocumentUploadKind.Succeeded,
                new CreateDocumentUploadResult(
                    existing.Id,
                    existing.Status,
                    replayGrant.Url,
                    replayGrant.Token,
                    replayGrant.Method),
                null);
        }

        var documentId = Guid.NewGuid();
        var document = new CaseDocument
        {
            Id = documentId,
            TenantId = _currentBroker.TenantId,
            CaseId = command.CaseId,
            UploadedByBrokerId = _currentBroker.BrokerId,
            Type = command.Type,
            Status = DocumentStatus.PendingUpload,
            Sensitivity = CaseDocument.SensitivityFor(command.Type),
            OriginalFileName = fileName,
            ContentType = command.ContentType.Trim(),
            SizeBytes = command.SizeBytes,
            Sha256 = sha256,
            LandingKey = _objectStore.LandingKey(
                _currentBroker.TenantId,
                command.CaseId,
                documentId,
                fileName),
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
        };

        await _documents.Add(document, cancellationToken);
        await _accessLogs.Add(
            DocumentAccess.Log(document, _currentBroker.BrokerId, DocumentAccessAction.UploadUrlIssued),
            cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);

        var grant = _objectStore.CreateUploadGrant(document.Id, document.TenantId, UploadTtl);
        return new CreateDocumentUploadOutcome(
            CreateDocumentUploadKind.Succeeded,
            new CreateDocumentUploadResult(
                document.Id,
                document.Status,
                grant.Url,
                grant.Token,
                grant.Method),
            null);
    }

    private static string? Validate(CreateDocumentUploadCommand command)
    {
        if (command.CaseId == Guid.Empty)
            return "CaseId is required.";

        var fileName = Path.GetFileName(command.FileName ?? string.Empty).Trim();
        if (fileName.Length == 0)
            return "FileName is required.";

        if (string.IsNullOrWhiteSpace(command.ContentType)
            || !DocumentRules.AllowedContentTypes.Contains(command.ContentType.Trim()))
            return "Content type is not allowed.";

        if (command.SizeBytes is <= 0 or > DocumentRules.MaxBytes)
            return "File size is not allowed.";

        if (string.IsNullOrWhiteSpace(command.Sha256)
            || !DocumentRules.IsSha256(command.Sha256.Trim()))
            return "Sha256 must be a 64-character hex digest.";

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return "IdempotencyKey is required.";

        return null;
    }

    private static bool SameRequest(
        CaseDocument existing,
        CreateDocumentUploadCommand command,
        string fileName,
        string sha256) =>
        existing.CaseId == command.CaseId
        && existing.Type == command.Type
        && existing.OriginalFileName == fileName
        && existing.ContentType.Equals(command.ContentType.Trim(), StringComparison.OrdinalIgnoreCase)
        && existing.SizeBytes == command.SizeBytes
        && existing.Sha256 == sha256;
}