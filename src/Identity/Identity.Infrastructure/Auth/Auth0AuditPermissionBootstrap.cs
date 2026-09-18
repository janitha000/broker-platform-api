using Identity.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Auth;

public sealed class Auth0AuditPermissionBootstrap : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Auth0Options _options;
    private readonly ILogger<Auth0AuditPermissionBootstrap> _logger;

    public Auth0AuditPermissionBootstrap(
        IServiceScopeFactory scopeFactory,
        IOptions<Auth0Options> options,
        ILogger<Auth0AuditPermissionBootstrap> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ManagementClientId)
            || string.IsNullOrWhiteSpace(_options.ManagementClientSecret))
            return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var directory = scope.ServiceProvider.GetRequiredService<IAuth0OrganizationDirectory>();
            await directory.EnsureAuditReadPermission(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not grant Auth0 audit:read on Principal");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
