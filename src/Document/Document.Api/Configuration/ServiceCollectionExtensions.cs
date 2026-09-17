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
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
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
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter()));
        services.AddCorsFromConfiguration(configuration);
        return services;
    }

    private static IServiceCollection AddCorsFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (corsOrigins.Length > 0)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        return services;
    }
}