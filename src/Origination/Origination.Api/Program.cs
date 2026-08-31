using Origination.Api.Configuration;
using Origination.Application;
using Origination.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOriginationApi(builder.Configuration);

var app = builder.Build();
app.UseOriginationApi();
app.Run();
