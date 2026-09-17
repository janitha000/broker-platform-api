using Document.Application.Documents;

namespace Document.Application.Documents.ListDocuments;

public sealed record ListDocumentsResult(IReadOnlyList<CaseDocumentItem> Documents);