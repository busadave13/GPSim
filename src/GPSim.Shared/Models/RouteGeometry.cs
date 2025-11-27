namespace GPSim.Shared.Models;

/// <summary>
/// Route geometry from Mapbox Directions API
/// </summary>
public record RouteGeometry
{
    public string Type { get; init; } = "LineString";
    public List<double[]> Coordinates { get; init; } = new();
    public double DistanceMeters { get; init; }
    public double DurationSeconds { get; init; }
}
