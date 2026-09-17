namespace Document.Application.Documents.ScanLandedObject;

public sealed class DocumentNotReadyException : Exception
{
    public DocumentNotReadyException(string message) : base(message) { }
}