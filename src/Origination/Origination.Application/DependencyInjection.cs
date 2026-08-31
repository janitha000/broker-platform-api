using Origination.Application.Cases.CompleteFactFind;
using Origination.Application.Cases.CreateCase;
using Origination.Application.Cases.GetCase;
using Origination.Application.Cases.GetCases;
using Microsoft.Extensions.DependencyInjection;

namespace Origination.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateCaseHandler>();
        services.AddScoped<GetCaseHandler>();
        services.AddScoped<GetCasesHandler>();
        services.AddScoped<CompleteFactFindHandler>();
        return services;
    }
}
