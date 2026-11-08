using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ShipBoard.Api;

namespace ShipBoard.Api.Tests;

// Test con il database in memoria: veloci, nessuna dipendenza esterna.
public class TicketApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Created_ticket_can_be_read_back()
    {
        var created = await _client.PostAsJsonAsync("/tickets", new { title = "Dockerizzare l'API", priority = "High" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var ticket = await created.Content.ReadFromJsonAsync<TicketDto>();
        var read = await _client.GetFromJsonAsync<TicketDto>($"/tickets/{ticket!.Id}");

        Assert.Equal("Dockerizzare l'API", read!.Title);
        Assert.Equal("High", read.Priority);
    }

    [Fact]
    public async Task Ticket_without_title_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/tickets", new { title = " " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Filter_by_priority_returns_only_matching_tickets()
    {
        await _client.PostAsJsonAsync("/tickets", new { title = "Bassa", priority = "Low" });
        await _client.PostAsJsonAsync("/tickets", new { title = "Alta", priority = "High" });

        var high = await _client.GetFromJsonAsync<List<TicketDto>>("/tickets?priority=High");

        Assert.NotEmpty(high!);
        Assert.All(high!, t => Assert.Equal("High", t.Priority));
    }

    private record TicketDto(int Id, string Title, string Priority, bool Done);
}
