using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Audit.Infrastructure.Persistence;

public sealed class AuditDatabaseInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditDatabaseInitializer> _logger;

    public AuditDatabaseInitializer(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditDatabaseInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var connectionString = db.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("ConnectionStrings:Audit empty; skip migrate");
            return;
        }

        try
        {
            await EnsureDatabase(connectionString, cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit database create/migrate failed");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureDatabase(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)
            || database.Equals("master", StringComparison.OrdinalIgnoreCase))
            return;

        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{Escape(database)}') IS NULL
                CREATE DATABASE [{Escape(database)}];
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string Escape(string name) => name.Replace("]", "]]").Replace("'", "''");
}
