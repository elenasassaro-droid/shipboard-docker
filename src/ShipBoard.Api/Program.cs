using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
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
