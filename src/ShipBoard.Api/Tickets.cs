using Microsoft.EntityFrameworkCore;

namespace ShipBoard.Api;

public enum Priority { Low, Medium, High }

public class Ticket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public bool Done { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public record CreateTicket(string Title, Priority Priority = Priority.Medium);

public class ShipBoardDb(DbContextOptions<ShipBoardDb> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
}

public static class TicketEndpoints
{
    public static void MapTickets(this WebApplication app)
    {
        var tickets = app.MapGroup("/tickets");

        tickets.MapGet("/", async (ShipBoardDb db, Priority? priority, IConfiguration config) =>
        {
            // using var: lo span si chiude da solo alla fine della lambda
            using var activity = Telemetry.Source.StartActivity("carica-ticket");

            // Il ritardo sta DENTRO lo span, cosi' la dashboard lo attribuisce a lui
            if (int.TryParse(config["DELAY_MS"], out var delay) && delay > 0)
                await Task.Delay(delay);

            var result = await db.Tickets
                .Where(t => priority == null || t.Priority == priority)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // activity puo' essere null se nessuno ascolta la sorgente, quindi ?.
            activity?.SetTag("tickets.count", result.Count);

            return result;
        });

        tickets.MapGet("/{id:int}", async (int id, ShipBoardDb db) =>
            await db.Tickets.FindAsync(id) is { } t ? Results.Ok(t) : Results.NotFound());

        tickets.MapPost("/", async (CreateTicket input, ShipBoardDb db, ILogger<Program> logger) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = ["Il titolo è obbligatorio."]
                });

            var ticket = new Ticket { Title = input.Title.Trim(), Priority = input.Priority };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();   // da qui in poi ticket.Id e' valorizzato

            // Contatore con tag "priority": una serie per Low/Medium/High
            Telemetry.TicketsCreated.Add(1,
                new KeyValuePair<string, object?>("priority", ticket.Priority.ToString()));

            // Log strutturato: {TicketId} e {Priority} diventano campi filtrabili
            logger.LogInformation("Creato il ticket {TicketId} con priorita' {Priority}",
                ticket.Id, ticket.Priority);

            return Results.Created($"/tickets/{ticket.Id}", ticket);
        });

        tickets.MapPost("/{id:int}/done", async (int id, ShipBoardDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();
            ticket.Done = true;
            await db.SaveChangesAsync();
            return Results.Ok(ticket);
        });
    }
}