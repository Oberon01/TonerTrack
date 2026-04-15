using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TonerTrack.Api.Extensions;
using TonerTrack.Api.Middleware;
using TonerTrack.Application.Discovery;
using TonerTrack.Infrastructure.DependencyInjection;
using TonerTrack.Infrastructure.NinjaRmm;
using TonerTrack.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true,  reloadOnChange: true)
    .AddEnvironmentVariables();

// Add logging providers
builder.Logging.ClearProviders().AddConsole().AddDebug();

// Adding services to the container
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IValidateOptions<NinjaRmmOptions>, NinjaRmmOptionsValidator>();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "TonerTrack API", Version = "v1" }));

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("data_file", () =>
    {
        var opts = builder.Configuration
            .GetSection(JsonPersistenceOptions.Section)
            .Get<JsonPersistenceOptions>() ?? new();
        var dir = opts.DataDirectory;
        return Directory.Exists(dir)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded($"Data directory not found: {dir}");
    });

// Pipelines
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles();   // serves wwwroot/ 
app.MapControllers();
app.MapHealthChecks("/health");

// banner
var log = app.Services.GetRequiredService<ILogger<Program>>();
log.LogInformation("========================================");
log.LogInformation("  TonerTrack .NET 10  |  {Env}", app.Environment.EnvironmentName);
log.LogInformation("========================================");

var handler = app.Services.GetService<IRequestHandler<DiscoverPrintersCommand, DiscoveryResult>>();
log.LogInformation("Discovery handler registered: {Found}", handler is not null);

app.Run();

// Expose Program for integration tests
public partial class Program { }
