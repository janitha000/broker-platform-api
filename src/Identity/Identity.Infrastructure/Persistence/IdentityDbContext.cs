using Identity.Domain.Tenants;
using Identity.Domain.Registration;
using Identity.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<BrokerUser> BrokerUsers => Set<BrokerUser>();
    public DbSet<RegistrationSaga> RegistrationSagas => Set<RegistrationSaga>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new BrokerUserConfiguration());
        modelBuilder.ApplyConfiguration(new RegistrationSagaConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
