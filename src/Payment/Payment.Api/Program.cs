using Payment.Api.Configuration;
using Payment.Application;
using Payment.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPaymentApi(builder.Configuration);

var app = builder.Build();
app.UsePaymentApi();
app.Run();
