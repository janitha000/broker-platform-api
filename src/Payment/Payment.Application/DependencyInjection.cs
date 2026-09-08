using Payment.Application.Charges.CreateCharge;
using Payment.Application.Charges.RefundCharge;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateChargeHandler>();
        services.AddScoped<RefundChargeHandler>();
        return services;
    }
}
