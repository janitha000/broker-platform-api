using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Broker.Hosting.Audit;

public sealed class AuditDenyResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly AuthorizationMiddlewareResultHandler Inner = new();

    private readonly ILogger<AuditDenyResultHandler> _logger;

    public AuditDenyResultHandler(ILogger<AuditDenyResultHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
            await TryRecordDeny(context);

        await Inner.HandleAsync(next, context, policy, authorizeResult);
    }

    private async Task TryRecordDeny(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health/", StringComparison.OrdinalIgnoreCase))
            return;

        var recorder = context.RequestServices.GetService<IAuditRecorder>();
        if (recorder is null)
            return;

        try
        {
            var user = context.User;
            recorder.Record(new AuditEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                TenantId = ParseGuid(user, "tenant_id", "https://api.broker-platform.com/tenant_id")
                    ?? Guid.Empty,
                Action = AuditActions.AuthzDeny,
                Outcome = AuditOutcomes.Deny,
                Actor = new AuditActor
                {
                    Type = AuditActorTypes.User,
                    BrokerId = ParseGuid(user, "broker_id", "https://api.broker-platform.com/broker_id"),
                    Subject = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                },
                Resource = new AuditResource
                {
                    Type = AuditResourceTypes.Endpoint,
                    Id = $"{context.Request.Method} {path}",
                },
                CorrelationId = context.TraceIdentifier,
                RequestId = context.TraceIdentifier,
            });
            await recorder.Flush(context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "audit_deny_persist_failed");
        }
    }

    private static Guid? ParseGuid(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var type in claimTypes)
        {
            var value = user.FindFirst(type)?.Value;
            if (Guid.TryParse(value, out var id))
                return id;
        }

        return null;
    }
}