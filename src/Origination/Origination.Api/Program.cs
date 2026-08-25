using Origination.Api.Auth;
using Origination.Application.Abstractions;
using Origination.Domain.Cases;
using Origination.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Origination.Application.Cases.CreateCase;
using Origination.Application.Cases.GetCase;
using Origination.Application.Cases.CompleteFactFind;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ICurrentBroker, StubCurrentBroker>();
builder.Services.AddScoped<ICaseRepository, CaseRepository>();
builder.Services.AddScoped<CreateCaseHandler>();
builder.Services.AddScoped<GetCaseHandler>();
builder.Services.AddScoped<CompleteFactFindHandler>();


builder.Services.AddDbContext<OriginationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Origination")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.MapControllers();

app.Run();
