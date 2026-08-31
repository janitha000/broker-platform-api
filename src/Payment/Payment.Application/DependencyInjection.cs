using Payment.Application.Charges.CreateCharge;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateChargeHandler>();
        return services;
    }
}
