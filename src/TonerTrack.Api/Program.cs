using System.Text.Json;
using System.Text.Json.Serialization;
using TonerTrack.Api.Extensions;
using TonerTrack.Api.Middleware;
using TonerTrack.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
builder.Configuration
    .AddJsonFile("appsettings.json",                          optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
                                                              optional: true,  reloadOnChange: true)
    .AddEnvironmentVariables();

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders().AddConsole().AddDebug();

// ── Services ──────────────────────────────────────────────────────────────────
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
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "TonerTrack API", Version = "v1" }));

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles();   // serves wwwroot/ — drop your existing HTML/CSS/JS here
app.MapControllers();

// ── Startup banner ────────────────────────────────────────────────────────────
var log = app.Services.GetRequiredService<ILogger<Program>>();
log.LogInformation("========================================");
log.LogInformation("  TonerTrack .NET 10  |  {Env}", app.Environment.EnvironmentName);
log.LogInformation("========================================");

app.Run();

// Expose Program for integration tests
public partial class Program { }
