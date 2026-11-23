using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ShipBoard.Api;

var builder = WebApplication.CreateBuilder(args);

// Con una connection string usa PostgreSQL, altrimenti un database in memoria.
// Cosi' l'app parte con un semplice `dotnet run`, anche senza Docker.
var connectionString = builder.Configuration.GetConnectionString("shipboard");
builder.Services.AddDbContext<ShipBoardDb>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
        options.UseInMemoryDatabase("shipboard");
    else
        options.UseNpgsql(connectionString);
});

builder.Services.AddHealthChecks().AddDbContextCheck<ShipBoardDb>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// OpenTelemetry (lab 2, sezione 8, passo 3)
builder.Logging.AddOpenTelemetry(o => { o.IncludeFormattedMessage = true; o.IncludeScopes = true; });
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource("ShipBoard.Api")
        .AddSource("Npgsql")
        .AddAspNetCoreInstrumentation(o => o.Filter = ctx => ctx.Request.Path != "/health")
        .AddHttpClientInstrumentation())
    .WithMetrics(m => m
        .AddMeter("ShipBoard.Api")
        .AddAspNetCoreInstrumentation());
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
    builder.Services.AddOpenTelemetry().UseOtlpExporter();

var app = builder.Build();

// Crea lo schema all'avvio: comodo a lezione, in produzione si usano le migration.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ShipBoardDb>().Database.EnsureCreatedAsync();
}

app.MapGet("/", () => Results.Redirect("/info"));

// Utile per vedere "dove" gira l'app: dentro un container l'hostname e' l'id del container.
app.MapGet("/info", (IConfiguration config) => new
{
    app = "ShipBoard.Api",
    version = config["APP_VERSION"] ?? "dev",
    host = Environment.MachineName,
    user = Environment.UserName,
    os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    database = string.IsNullOrWhiteSpace(connectionString) ? "in-memory" : "postgresql"
});

app.MapHealthChecks("/health");
app.MapTickets();

app.Run();

public partial class Program;