using Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Auth;

public sealed class Auth0PaymentTokenProvider
{
    public const string ChargeScope = "payments:charge";

    private readonly HttpClient _http;
    private readonly Auth0Options _options;
    private readonly Auth0PaymentTokenCache _cache;

    public Auth0PaymentTokenProvider(
        HttpClient http,
        IOptions<Auth0Options> options,
        Auth0PaymentTokenCache cache)
    {
        _http = http;
        _options = options.Value;
        _cache = cache;
    }

    public Task<string?> GetAccessToken(CancellationToken cancellationToken = default) =>
        Auth0ClientCredentials.GetAccessToken(
            _http,
            _cache,
            _options.PaymentClientId,
            _options.PaymentClientSecret,
            _options.PaymentAudience,
            ChargeScope,
            cancellationToken);
}
