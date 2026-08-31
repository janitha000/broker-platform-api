using Identity.Api.Configuration;
using Identity.Application;
using Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityApi(builder.Configuration);

var app = builder.Build();
app.UseIdentityApi();
app.Run();
