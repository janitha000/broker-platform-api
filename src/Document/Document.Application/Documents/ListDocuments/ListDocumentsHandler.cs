using Document.Application.Abstractions;
using Document.Application.Auth;
using Document.Domain.Documents;

namespace Document.Application.Documents.ListDocuments;

public sealed class ListDocumentsHandler
{
    private readonly ICaseDocumentRepository _documents;
    private readonly ICurrentBroker _currentBroker;

    public ListDocumentsHandler(
        ICaseDocumentRepository documents,
        ICurrentBroker currentBroker)
    {
        _documents = documents;
        _currentBroker = currentBroker;
    }

    public async Task<ListDocumentsResult> Handle(
        ListDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var canReadSensitive = _currentBroker.HasPermission(DocumentPermissions.SensitiveRead);
        var documents = await _documents.ListByCase(
            _currentBroker.TenantId,
            query.CaseId,
            cancellationToken);

        return new ListDocumentsResult(
            documents.Select(d => DocumentAccess.ToItem(d, canReadSensitive)).ToList());
    }
}