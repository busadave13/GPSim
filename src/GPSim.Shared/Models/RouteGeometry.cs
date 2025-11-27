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

    /// <summary>
    /// Speed limits for each segment in mph (null means unknown)
    /// The count matches the number of segments (Coordinates.Count - 1)
    /// </summary>
    public List<int?> SpeedLimits { get; init; } = new();
}
