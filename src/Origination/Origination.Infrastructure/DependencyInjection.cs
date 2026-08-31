using Origination.Domain.Cases;
using Origination.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Origination.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddDbContext<OriginationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Origination")));
        return services;
    }
}
