using Broker.Hosting;
using Document.Api.Auth;
using Document.Application.Abstractions;
using Document.Application.Auth;

namespace Document.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBrokerWebHost(configuration, new BrokerWebHostOptions { ServiceName = "document-api" });

        services.AddHttpContextAccessor();
        services.AddScoped<JwtCurrentBroker>();
        services.AddScoped<ICurrentBroker>(sp => sp.GetRequiredService<JwtCurrentBroker>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<JwtCurrentBroker>());

        services.AddBrokerSessionAuthentication(configuration);
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                DocumentAuth.ReadPolicy,
                policy => policy.RequireAssertion(ctx =>
                    DocumentAuth.HasPermission(ctx.User, DocumentPermissions.Read)));
            options.AddPolicy(
                DocumentAuth.UploadPolicy,
                policy => policy.RequireAssertion(ctx =>
                    DocumentAuth.HasPermission(ctx.User, DocumentPermissions.Upload)));
            options.AddPolicy(
                DocumentAuth.SensitiveReadPolicy,
                policy => policy.RequireAssertion(ctx =>
                    DocumentAuth.HasPermission(ctx.User, DocumentPermissions.SensitiveRead)));
        });
        return services;
    }
}
