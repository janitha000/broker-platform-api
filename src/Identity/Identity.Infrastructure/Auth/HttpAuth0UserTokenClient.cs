using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Auth;

public sealed class HttpAuth0UserTokenClient : IAuth0UserTokenClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly Auth0Options _options;

    public HttpAuth0UserTokenClient(HttpClient http, IOptions<Auth0Options> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<Auth0RefreshedTokens?> Refresh(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["refresh_token"] = refreshToken,
                ["audience"] = _options.Audience,
            });

            using var response = await _http.PostAsync("oauth/token", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, cancellationToken);
            if (body is null || string.IsNullOrWhiteSpace(body.AccessToken))
                return null;

            return new Auth0RefreshedTokens(
                body.AccessToken,
                body.RefreshToken,
                body.ExpiresIn);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    public async Task Revoke(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["token"] = refreshToken,
            });
            await _http.PostAsync("oauth/revoke", content, cancellationToken);
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
