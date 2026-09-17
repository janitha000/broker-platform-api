using Amazon;
using Amazon.S3;
using Document.Application.Abstractions;
using Document.Domain.Abstractions;
using Document.Domain.Documents;
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
        services.AddScoped<IObjectStore, S3ObjectStore>();
        services.AddSingleton<IMalwareScanner, AllowlistMalwareScanner>();
        services.AddHostedService<LandingObjectCreatedWorker>();

        return services;
    }
}