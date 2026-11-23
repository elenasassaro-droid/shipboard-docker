using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ShipBoard.Api;

public static class Telemetry
{
    // Deve coincidere con .AddSource("ShipBoard.Api") e .AddMeter("ShipBoard.Api") in Program.cs
    public const string Name = "ShipBoard.Api";

    public static readonly ActivitySource Source = new(Name);
    public static readonly Meter Meter = new(Name);

    public static readonly Counter<long> TicketsCreated =
        Meter.CreateCounter<long>("shipboard.tickets.created");
}