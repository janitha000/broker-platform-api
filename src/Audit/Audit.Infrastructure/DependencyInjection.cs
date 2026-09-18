using Amazon;
using Amazon.S3;
using Audit.Application.Abstractions;
using Audit.Infrastructure.Messaging;
using Audit.Infrastructure.Persistence;
using Audit.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuditDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Audit")));
        services.AddScoped<IAuditStore, AuditStore>();

        services.Configure<ArchiveOptions>(configuration.GetSection(ArchiveOptions.SectionName));
        services.Configure<SqsWorkerOptions>(configuration.GetSection(SqsWorkerOptions.SectionName));

        var region = configuration["Archive:AwsRegion"]
            ?? configuration["Messaging:AwsRegion"]
            ?? "ap-southeast-2";
        services.AddSingleton<IAmazonS3>(_ =>
            new AmazonS3Client(RegionEndpoint.GetBySystemName(region)));
        services.AddSingleton<IAuditArchive, S3AuditArchive>();
        services.AddHostedService<AuditDatabaseInitializer>();
        services.AddHostedService<AuditQueueWorker>();

        return services;
    }
}
