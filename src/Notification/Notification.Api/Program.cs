using Notification.Api.Configuration;
using Notification.Application;
using Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddNotificationApi(builder.Configuration);

var app = builder.Build();
app.UseNotificationApi();
app.Run();
