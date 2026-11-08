// dotnet run --project src/ShipBoard.AppHost  ->  apre la dashboard di Aspire
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL in un container, con volume per i dati e pgAdmin per curiosare nel database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();
var db = postgres.AddDatabase("shipboard");

// L'API gira come processo .NET; Aspire le passa la connection string "shipboard"
var api = builder.AddProject<Projects.ShipBoard_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithHttpHealthCheck("/health");

// Il frontend viene costruito dal suo Dockerfile, come in compose
builder.AddDockerfile("web", "../../web")
    .WithHttpEndpoint(targetPort: 8080)
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
