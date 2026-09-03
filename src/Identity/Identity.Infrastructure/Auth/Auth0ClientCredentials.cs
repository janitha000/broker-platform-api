using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Identity.Infrastructure.Auth;

internal static class Auth0ClientCredentials
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<string?> GetAccessToken(
        HttpClient http,
        Auth0TokenCache cache,
        string clientId,
        string clientSecret,
        string audience,
        string? scope,
        CancellationToken cancellationToken)
    {
        if (cache.AccessToken is not null
            && cache.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return cache.AccessToken;

        await cache.Gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.AccessToken is not null
                && cache.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
                return cache.AccessToken;

            var fields = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["audience"] = audience,
            };
            if (!string.IsNullOrWhiteSpace(scope))
                fields["scope"] = scope;

            using var content = new FormUrlEncodedContent(fields);
            using var response = await http.PostAsync("oauth/token", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadFromJsonAsync<Auth0TokenResponse>(Json, cancellationToken);
            if (string.IsNullOrWhiteSpace(body?.AccessToken))
                return null;

            cache.AccessToken = body.AccessToken;
            cache.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(body.ExpiresIn - 30, 60));
            return body.AccessToken;
        }
        finally
        {
            cache.Gate.Release();
        }
    }

    private sealed class Auth0TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
