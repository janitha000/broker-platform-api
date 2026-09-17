using Document.Application.Documents.CompleteDocumentUpload;
using Document.Application.Documents.CreateDocumentUpload;
using Document.Application.Documents.ListDocuments;
using Document.Application.Documents.RequestDocumentDownload;
using Document.Application.Documents.ScanLandedObject;
using Document.Application.Documents.ScanPendingDocuments;
using Microsoft.Extensions.DependencyInjection;

namespace Document.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateDocumentUploadHandler>();
        services.AddScoped<CompleteDocumentUploadHandler>();
        services.AddScoped<ListDocumentsHandler>();
        services.AddScoped<RequestDocumentDownloadHandler>();
        services.AddScoped<ScanPendingDocumentsHandler>();
        services.AddScoped<ScanLandedObjectHandler>();
        return services;
    }
}