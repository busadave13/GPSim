namespace GPSim.Shared.Models;

/// <summary>
/// GPS payload sent to webhook endpoint
/// </summary>
public record GpsPayload
{
    /// <summary>
    /// Device/simulator identifier
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// Latitude in decimal degrees
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Longitude in decimal degrees
    /// </summary>
    public double Longitude { get; init; }

    /// <summary>
    /// Altitude in meters (optional)
    /// </summary>
    public double? Altitude { get; init; }

    /// <summary>
    /// Speed in meters per second
    /// </summary>
    public double? Speed { get; init; }

    /// <summary>
    /// Bearing/heading in degrees (0-360)
    /// </summary>
    public double? Bearing { get; init; }

    /// <summary>
    /// GPS accuracy in meters
    /// </summary>
    public double? Accuracy { get; init; }

    /// <summary>
    /// Timestamp of the GPS reading
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Sequence number for ordering
    /// </summary>
    public int SequenceNumber { get; init; }

    /// <summary>
    /// Optional simulation ID for tracking
    /// </summary>
    public Guid? SimulationId { get; init; }
}
