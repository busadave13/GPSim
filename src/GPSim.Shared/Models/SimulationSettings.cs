namespace GPSim.Shared.Models;

/// <summary>
/// Simulation configuration settings
/// </summary>
public class SimulationSettings
{
    /// <summary>
    /// Interval between GPS updates in milliseconds
    /// </summary>
    public int IntervalMs { get; set; } = 1000;

    /// <summary>
    /// Speed multiplier for simulation (1.0 = real-time)
    /// </summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Device identifier for GPS payloads
    /// </summary>
    public string DeviceId { get; set; } = "simulator";

    /// <summary>
    /// Optional webhook URL override
    /// </summary>
    public string? WebhookUrl { get; set; }
}
