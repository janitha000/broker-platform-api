using Identity.Api.Auth;
using Identity.Application.Abstractions;
using Identity.Application.Tenants.Login;
using Identity.Application.Tenants.RegisterTenant;
using Identity.Domain.Tenants;
using Identity.Infrastructure.Auth;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IBrokerUserRepository, BrokerUserRepository>();
builder.Services.AddScoped<RegisterTenantHandler>();
builder.Services.AddScoped<LoginHandler>();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Identity")));

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
builder.Services.AddAuthorization();

builder.Services.AddControllers();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (corsOrigins.Length > 0)
    app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
