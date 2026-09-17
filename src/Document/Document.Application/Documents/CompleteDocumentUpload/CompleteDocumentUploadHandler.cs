using System.Security.Cryptography;
using Broker.Hosting.Audit;
using Document.Application.Abstractions;
using Document.Application.Auth;
using Document.Domain.Abstractions;
using Document.Domain.Documents;

namespace Document.Application.Documents.CompleteDocumentUpload;

public sealed class CompleteDocumentUploadHandler
{
    private readonly ICaseDocumentRepository _documents;
    private readonly IObjectStore _objectStore;
    private readonly ICurrentBroker _currentBroker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditRecorder _audit;

    public CompleteDocumentUploadHandler(
        ICaseDocumentRepository documents,
        IObjectStore objectStore,
        ICurrentBroker currentBroker,
        IUnitOfWork unitOfWork,
        IAuditRecorder audit)
    {
        _documents = documents;
        _objectStore = objectStore;
        _currentBroker = currentBroker;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task<CompleteDocumentUploadOutcome> Handle(
        CompleteDocumentUploadCommand command,
        CancellationToken cancellationToken = default)
    {
        var document = await _documents.GetById(
            command.DocumentId,
            _currentBroker.TenantId,
            cancellationToken);

        if (document is null)
            return new CompleteDocumentUploadOutcome(CompleteDocumentUploadKind.NotFound, null, null);

        if (document.Status != DocumentStatus.PendingUpload)
        {
            return new CompleteDocumentUploadOutcome(
                CompleteDocumentUploadKind.Conflict,
                null,
                "Upload can only be completed from PendingUpload.");
        }

        if (!await _objectStore.LandingExists(document.LandingKey, cancellationToken))
        {
            return new CompleteDocumentUploadOutcome(
                CompleteDocumentUploadKind.Invalid,
                null,
                "No file has been uploaded yet.");
        }

        var bytes = await _objectStore.ReadLandingBytes(document.LandingKey, cancellationToken);
        if (bytes.Length != document.SizeBytes)
        {
            return new CompleteDocumentUploadOutcome(
                CompleteDocumentUploadKind.Invalid,
                null,
                "Uploaded size does not match the declared size.");
        }

        var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!digest.Equals(document.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            return new CompleteDocumentUploadOutcome(
                CompleteDocumentUploadKind.Invalid,
                null,
                "Uploaded checksum does not match the declared SHA-256.");
        }

        document.Status = DocumentStatus.PendingScan;
        document.UploadedAt = DateTime.UtcNow;
        await _documents.Update(document, cancellationToken);
        _audit.Record(DocumentAudit.For(
            document, _currentBroker.BrokerId, AuditActions.DocumentUploadCompleted));
        await _unitOfWork.SaveChanges(cancellationToken);

        var canReadSensitive = _currentBroker.HasPermission(DocumentPermissions.SensitiveRead);
        return new CompleteDocumentUploadOutcome(
            CompleteDocumentUploadKind.Succeeded,
            DocumentAccess.ToItem(document, canReadSensitive),
            null);
    }
}
