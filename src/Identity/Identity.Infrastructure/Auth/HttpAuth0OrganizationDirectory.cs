using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity.Application.Abstractions;
using Identity.Domain.Tenants;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Auth;

public sealed class HttpAuth0OrganizationDirectory : IAuth0OrganizationDirectory
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly Auth0Options _options;
    private readonly Auth0ManagementTokenCache _tokenCache;

    public HttpAuth0OrganizationDirectory(
        HttpClient http,
        IOptions<Auth0Options> options,
        Auth0ManagementTokenCache tokenCache)
    {
        _http = http;
        _options = options.Value;
        _tokenCache = tokenCache;
    }

    public async Task<Auth0OrganizationResult> CreateOrganization(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetManagementToken(cancellationToken);
            if (token is null)
                return new Auth0OrganizationResult(false, null);

            var slug = $"t{Guid.NewGuid():N}"[..16];
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/organizations");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new
            {
                name = slug,
                display_name = displayName.Trim(),
            });

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new Auth0OrganizationResult(false, null);

            var created = await response.Content.ReadFromJsonAsync<OrganizationResponse>(Json, cancellationToken);
            if (string.IsNullOrWhiteSpace(created?.Id))
                return new Auth0OrganizationResult(false, null);

            var enabled = await EnableDatabaseConnection(token, created.Id, cancellationToken);
            return enabled
                ? new Auth0OrganizationResult(true, created.Id)
                : new Auth0OrganizationResult(false, null);
        }
        catch (HttpRequestException)
        {
            return new Auth0OrganizationResult(false, null);
        }
        catch (TaskCanceledException)
        {
            return new Auth0OrganizationResult(false, null);
        }
    }

    public async Task<bool> AddMemberWithPrincipalRole(
        string organizationId,
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetManagementToken(cancellationToken);
            if (token is null)
                return false;

            var roleId = await ResolvePrincipalRoleId(token, cancellationToken);
            if (string.IsNullOrWhiteSpace(roleId))
                return false;

            using (var members = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/v2/organizations/{Uri.EscapeDataString(organizationId)}/members"))
            {
                members.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                members.Content = JsonContent.Create(new { members = new[] { auth0UserId } });
                using var membersResponse = await _http.SendAsync(members, cancellationToken);
                if (!membersResponse.IsSuccessStatusCode
                    && membersResponse.StatusCode != System.Net.HttpStatusCode.Conflict)
                    return false;
            }

            using var roles = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/v2/organizations/{Uri.EscapeDataString(organizationId)}/members/{Uri.EscapeDataString(auth0UserId)}/roles");
            roles.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            roles.Content = JsonContent.Create(new { roles = new[] { roleId } });
            using var rolesResponse = await _http.SendAsync(roles, cancellationToken);
            if (!rolesResponse.IsSuccessStatusCode)
                return false;

            await EnsureAuditReadPermission(cancellationToken);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task EnsureAuditReadPermission(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetManagementToken(cancellationToken);
            if (token is null)
                return;

            await EnsureApiScope(token, cancellationToken);
            var roleId = await ResolvePrincipalRoleId(token, cancellationToken);
            if (string.IsNullOrWhiteSpace(roleId))
                return;

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/v2/roles/{Uri.EscapeDataString(roleId)}/permissions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new
            {
                permissions = new[]
                {
                    new
                    {
                        resource_server_identifier = _options.Audience,
                        permission_name = BrokerPermissions.AuditRead,
                    },
                },
            });
            using var response = await _http.SendAsync(request, cancellationToken);
            _ = response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task EnsureApiScope(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Audience))
            return;

        var identifier = Uri.EscapeDataString(_options.Audience);
        using var get = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v2/resource-servers?identifier={identifier}");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var getResponse = await _http.SendAsync(get, cancellationToken);
        if (!getResponse.IsSuccessStatusCode)
            return;

        var servers = await getResponse.Content.ReadFromJsonAsync<ResourceServerResponse[]>(Json, cancellationToken);
        var server = servers?.FirstOrDefault();
        if (server?.Id is null)
            return;

        var scopes = server.Scopes?.ToList() ?? [];
        if (scopes.Any(s => string.Equals(s.Value, BrokerPermissions.AuditRead, StringComparison.Ordinal)))
            return;

        scopes.Add(new ScopeResponse
        {
            Value = BrokerPermissions.AuditRead,
            Description = "Read tenant audit timeline",
        });

        using var patch = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/v2/resource-servers/{Uri.EscapeDataString(server.Id)}");
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        patch.Content = JsonContent.Create(new
        {
            scopes = scopes.Select(s => new { value = s.Value, description = s.Description }),
        });
        using var patchResponse = await _http.SendAsync(patch, cancellationToken);
        _ = patchResponse.IsSuccessStatusCode;
    }

    private async Task<bool> EnableDatabaseConnection(
        string token,
        string organizationId,
        CancellationToken cancellationToken)
    {
        var connectionId = await FindDatabaseConnectionId(token, cancellationToken);
        if (connectionId is null)
            return false;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/v2/organizations/{Uri.EscapeDataString(organizationId)}/enabled_connections");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            connection_id = connectionId,
            assign_membership_on_login = false,
        });
        using var response = await _http.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict;
    }

    private async Task<string?> FindDatabaseConnectionId(string token, CancellationToken cancellationToken)
    {
        var name = Uri.EscapeDataString(_options.DatabaseConnection);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v2/connections?strategy=auth0&name={name}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var connections = await response.Content.ReadFromJsonAsync<ConnectionResponse[]>(Json, cancellationToken);
        return connections?.FirstOrDefault()?.Id;
    }

    private async Task<string?> ResolvePrincipalRoleId(string token, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.PrincipalRoleId))
            return _options.PrincipalRoleId.Trim();

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/roles?name_filter=Principal&per_page=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var roles = await response.Content.ReadFromJsonAsync<RoleResponse[]>(Json, cancellationToken);
        return roles?.FirstOrDefault(r =>
            string.Equals(r.Name, BrokerRoleName, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private Task<string?> GetManagementToken(CancellationToken cancellationToken) =>
        Auth0ClientCredentials.GetAccessToken(
            _http,
            _tokenCache,
            _options.ManagementClientId,
            _options.ManagementClientSecret,
            $"https://{_options.Domain}/api/v2/",
            scope: null,
            cancellationToken);

    private const string BrokerRoleName = "Principal";

    private sealed class OrganizationResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private sealed class ConnectionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private sealed class RoleResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class ResourceServerResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("scopes")]
        public ScopeResponse[]? Scopes { get; set; }
    }

    private sealed class ScopeResponse
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
