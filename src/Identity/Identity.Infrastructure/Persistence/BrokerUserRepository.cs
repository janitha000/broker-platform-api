using Identity.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class BrokerUserRepository : IBrokerUserRepository
{
    private readonly IdentityDbContext _context;

    public BrokerUserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<BrokerUser?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BrokerUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<BrokerUser?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _context.BrokerUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<BrokerUser?> GetByAuth0UserId(
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BrokerUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId, cancellationToken);
    }

    public async Task<BrokerUser> Add(BrokerUser brokerUser, CancellationToken cancellationToken = default)
    {
        _context.BrokerUsers.Add(brokerUser);
        await _context.SaveChangesAsync(cancellationToken);
        return brokerUser;
    }

    public async Task Update(BrokerUser brokerUser, CancellationToken cancellationToken = default)
    {
        _context.BrokerUsers.Update(brokerUser);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
