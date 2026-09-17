using Document.Api.Configuration;
using Document.Application;
using Document.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDocumentApi(builder.Configuration);

var app = builder.Build();
app.UseDocumentApi();
app.Run();
