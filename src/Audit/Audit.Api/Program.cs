using Audit.Api.Configuration;
using Audit.Application;
using Audit.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuditApi(builder.Configuration);

var app = builder.Build();
app.UseAuditApi();
app.Run();
