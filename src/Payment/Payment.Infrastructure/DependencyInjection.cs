using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions;
using Payment.Domain.Charges;
using Payment.Infrastructure.Payments;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IChargeRepository, InMemoryChargeRepository>();
        services.AddSingleton<ICardGateway, MockCardGateway>();
        return services;
    }
}