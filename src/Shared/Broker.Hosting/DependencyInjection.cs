using System.Text;
using Broker.Hosting.Audit;
using Broker.Hosting.Auth;
using Broker.Hosting.OpenApi;
using Broker.Hosting.RateLimiting;
using Broker.Hosting.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Broker.Hosting;

public static class DependencyInjection
{
    public static IServiceCollection AddBrokerSessionAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditDenyResultHandler>();
        return AuthModes.UseAuth0Organizations(configuration)
            ? services.AddAuth0AccessTokenAuthentication(configuration)
            : services.AddBrokerJwtAuthentication(configuration);
    }

    public static IServiceCollection AddBrokerJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        var signingKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                AuthCookie.ReadJwtFromCookie(options);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                };
            });
        return services;
    }

    public static IServiceCollection AddAuth0AccessTokenAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var domain = configuration["Auth0:Domain"]
            ?? throw new InvalidOperationException("Auth0:Domain is not configured.");
        var audience = configuration["Auth0:Audience"]
            ?? throw new InvalidOperationException("Auth0:Audience is not configured.");
        var authority = $"https://{domain}/";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.MapInboundClaims = false;
                AuthCookie.ReadJwtFromCookie(options);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = authority,
                    ValidIssuers = [authority, $"https://{domain}"],
                    ValidAudience = audience,
                };
            });
        return services;
    }

    public static IServiceCollection AddBrokerWebHost(
        this IServiceCollection services,
        IConfiguration configuration,
        BrokerWebHostOptions? options = null)
    {
        options ??= new BrokerWebHostOptions();

        services.AddBrokerTelemetry(configuration, options.ServiceName);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(swagger =>
        {
            swagger.SupportNonNullableReferenceTypes();
            swagger.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
        });
        services.AddControllers()
            .AddJsonOptions(json =>
                json.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter()));
        services.AddBrokerRateLimiting(configuration);

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (origins.Length > 0)
        {
            services.AddCors(cors =>
            {
                cors.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    if (options.AllowCredentials)
                        policy.AllowCredentials();
                });
            });
        }

        return services;
    }

    public static WebApplication UseBrokerWebHost(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        var origins = app.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (origins.Length > 0)
            app.UseCors();

        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
