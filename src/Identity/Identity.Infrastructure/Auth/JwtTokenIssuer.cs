using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Abstractions;
using Identity.Domain.Tenants;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Auth;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    public const string TenantIdClaim = "tenant_id";

    private readonly JwtOptions _options;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string Issue(Guid brokerId, Guid tenantId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, brokerId.ToString()),
            new(ClaimTypes.NameIdentifier, brokerId.ToString()),
            new(TenantIdClaim, tenantId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(BrokerPermissions.RoleClaimType, role),
        };

        foreach (var permission in BrokerPermissions.ForRole(role))
            claims.Add(new Claim(BrokerPermissions.ClaimType, permission));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
