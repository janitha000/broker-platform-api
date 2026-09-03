using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Auth;

public sealed class HttpAuth0UserDirectory : IAuth0UserDirectory
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly Auth0Options _options;
    private readonly Auth0ManagementTokenCache _tokenCache;

    public HttpAuth0UserDirectory(
        HttpClient http,
        IOptions<Auth0Options> options,
        Auth0ManagementTokenCache tokenCache)
    {
        _http = http;
        _options = options.Value;
        _tokenCache = tokenCache;
    }

    public async Task<Auth0ProvisionResult> ProvisionUser(
        string email,
        string password,
        Guid tenantId,
        Guid brokerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetManagementToken(cancellationToken);
            if (token is null)
                return new Auth0ProvisionResult(Auth0ProvisionKind.Failed, null);

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new
            {
                email,
                password,
                connection = _options.DatabaseConnection,
                verify_email = false,
                app_metadata = new
                {
                    tenant_id = tenantId.ToString(),
                    broker_id = brokerId.ToString(),
                },
            });

            var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Created
                || response.StatusCode == HttpStatusCode.OK)
            {
                var created = await response.Content.ReadFromJsonAsync<Auth0UserResponse>(Json, cancellationToken);
                if (string.IsNullOrWhiteSpace(created?.UserId))
                    return new Auth0ProvisionResult(Auth0ProvisionKind.Failed, null);
                return new Auth0ProvisionResult(Auth0ProvisionKind.Succeeded, created.UserId);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var existingId = await FindUserIdByEmail(token, email, cancellationToken);
                if (existingId is null)
                    return new Auth0ProvisionResult(Auth0ProvisionKind.Failed, null);
                return new Auth0ProvisionResult(Auth0ProvisionKind.Succeeded, existingId);
            }

            return new Auth0ProvisionResult(Auth0ProvisionKind.Failed, null);
        }
        catch (HttpRequestException)
        {
            return new Auth0ProvisionResult(Auth0ProvisionKind.Failed, null);
        }
        catch (TaskCanceledException)
        {
            return new Auth0ProvisionResult(Auth0ProvisionKind.Failed, null);
        }
    }

    private async Task<string?> GetManagementToken(CancellationToken cancellationToken)
    {
        if (_tokenCache.AccessToken is not null
            && _tokenCache.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return _tokenCache.AccessToken;

        await _tokenCache.Gate.WaitAsync(cancellationToken);
        try
        {
            if (_tokenCache.AccessToken is not null
                && _tokenCache.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
                return _tokenCache.AccessToken;

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ManagementClientId,
                ["client_secret"] = _options.ManagementClientSecret,
                ["audience"] = $"https://{_options.Domain}/api/v2/",
            });

            using var response = await _http.PostAsync("oauth/token", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadFromJsonAsync<Auth0TokenResponse>(Json, cancellationToken);
            if (string.IsNullOrWhiteSpace(body?.AccessToken))
                return null;

            _tokenCache.AccessToken = body.AccessToken;
            _tokenCache.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(body.ExpiresIn - 30, 60));
            return body.AccessToken;
        }
        finally
        {
            _tokenCache.Gate.Release();
        }
    }

    private async Task<string?> FindUserIdByEmail(
        string token,
        string email,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v2/users-by-email?email={Uri.EscapeDataString(email)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var users = await response.Content.ReadFromJsonAsync<Auth0UserResponse[]>(Json, cancellationToken);
        return users?.FirstOrDefault()?.UserId;
    }

    private sealed class Auth0TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class Auth0UserResponse
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
    }
}

public sealed class Auth0ManagementTokenCache
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public string? AccessToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
