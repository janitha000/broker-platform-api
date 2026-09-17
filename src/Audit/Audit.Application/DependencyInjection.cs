using Audit.Application.Events.IngestAuditEvent;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IngestAuditEventHandler>();
        return services;
    }
}
