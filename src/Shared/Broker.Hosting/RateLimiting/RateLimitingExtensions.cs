using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Broker.Hosting.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddBrokerRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .Validate(
                options => options.PermitLimit > 0
                    && options.WindowSeconds > 0
                    && options.AuthPermitLimit > 0
                    && options.AuthWindowSeconds > 0,
                "Rate-limiting permit limits and windows must be greater than zero.")
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var retry = 60;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    retry = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                context.HttpContext.Response.Headers.RetryAfter = retry.ToString();
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://httpstatuses.com/429",
                        title = "Too many requests",
                        status = 429,
                        detail = "Rate limit exceeded. Wait and retry.",
                    },
                    cancellationToken);
            };

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
            {
                var options = http.RequestServices
                    .GetRequiredService<IOptionsSnapshot<RateLimitingOptions>>()
                    .Value;

                if (!options.Enabled || RateLimitPartitionKey.IsHealth(http))
                {
                    return RateLimitPartition.GetNoLimiter("off");
                }

                var auth = RateLimitPartitionKey.IsAuth(http);
                var scope = auth ? "auth" : "api";
                var key = $"{scope}:{RateLimitPartitionKey.For(http)}";
                var permit = auth ? options.AuthPermitLimit : options.PermitLimit;
                var window = TimeSpan.FromSeconds(
                    auth ? options.AuthWindowSeconds : options.WindowSeconds);

                return RateLimitPartition.GetSlidingWindowLimiter(
                    key,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permit,
                        Window = window,
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }
}
