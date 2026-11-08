using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace ShipBoard.Api.IntegrationTests;

// Avvia un vero PostgreSQL in un container per la durata dei test, poi lo butta via.
// Serve solo Docker: niente database installato sul PC o sul runner della CI.
public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => Postgres.StartAsync();
    public Task DisposeAsync() => Postgres.DisposeAsync().AsTask();
}

public class PostgresApiTests(PostgresFixture db) : IClassFixture<PostgresFixture>
{
    private HttpClient CreateClient() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:shipboard", db.Postgres.GetConnectionString()))
            .CreateClient();

    [Fact]
    public async Task Api_uses_postgres()
    {
        var info = await CreateClient().GetFromJsonAsync<Info>("/info");
        Assert.Equal("postgresql", info!.Database);
    }

    [Fact]
    public async Task Tickets_survive_an_api_restart()
    {
        // Prima "istanza" dell'API: crea un ticket
        var created = await CreateClient().PostAsJsonAsync("/tickets", new { title = "Persistente", priority = "Low" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        // Seconda istanza, stesso database: il ticket c'e' ancora
        var tickets = await CreateClient().GetFromJsonAsync<List<TicketDto>>("/tickets");
        Assert.Contains(tickets!, t => t.Title == "Persistente");
    }

    [Fact]
    public async Task Health_checks_the_database()
    {
        var response = await CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private record Info(string Database);
    private record TicketDto(int Id, string Title);
}
