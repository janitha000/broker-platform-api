using Amazon;
using Amazon.S3;
using Broker.Hosting.Audit;
using Document.Application.Abstractions;
using Document.Domain.Abstractions;
using Document.Domain.Documents;
using Document.Domain.Outbox;
using Document.Infrastructure.Messaging;
using Document.Infrastructure.Persistence;
using Document.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Document.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<S3StorageOptions>(configuration.GetSection(S3StorageOptions.SectionName));
        services.Configure<SqsWorkerOptions>(configuration.GetSection(SqsWorkerOptions.SectionName));

        var region = configuration["Storage:AwsRegion"] ?? "ap-southeast-2";
        services.AddSingleton<IAmazonS3>(_ =>
            new AmazonS3Client(RegionEndpoint.GetBySystemName(region)));

        services.AddDbContext<DocumentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Document")));

        services.AddScoped<ICaseDocumentRepository, CaseDocumentRepository>();
        services.AddScoped<IDocumentAccessLogRepository, DocumentAccessLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, Outbox>();
        services.AddScoped<IAuditRecorder, OutboxAuditRecorder>();
        services.AddScoped<IObjectStore, S3ObjectStore>();
        services.AddSingleton<IMalwareScanner, AllowlistMalwareScanner>();
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        var busProvider = configuration["Messaging:Provider"] ?? "Logging";
        if (string.Equals(busProvider, "EventBridge", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IMessageBus, EventBridgeMessageBus>();
        else
            services.AddSingleton<IMessageBus, LoggingMessageBus>();

        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<LandingObjectCreatedWorker>();

        return services;
    }
}