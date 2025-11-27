namespace GPSim.Shared.Models;

/// <summary>
/// Represents a complete simulation route configuration
/// </summary>
public record SimulationRoute
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public List<Coordinate> Waypoints { get; init; } = new();
    public RouteGeometry? Geometry { get; init; }
    public SimulationSettings Settings { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; init; }
}
