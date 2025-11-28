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
    /// Simulation speed in miles per hour
    /// </summary>
    public double SpeedMph { get; set; } = 30.0;

    /// <summary>
    /// Optional webhook URL override
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Optional custom headers to send with webhook requests
    /// Format: "Header1:Value1;Header2:Value2"
    /// </summary>
    public string? WebhookHeaders { get; set; }
}
