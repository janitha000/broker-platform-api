using Audit.Application.Events.IngestAuditEvent;
using Audit.Application.Events.ListAuditEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IngestAuditEventHandler>();
        services.AddScoped<ListAuditEventsHandler>();
        return services;
    }
}
