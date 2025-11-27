namespace GPSim.Server.Configuration;

/// <summary>
/// Mapbox configuration settings
/// </summary>
public class MapboxSettings
{
    public const string SectionName = "Mapbox";
    
    /// <summary>
    /// Mapbox API access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Radius of the stationary circle in miles (default: 0.1 = 1/10th mile)
    /// </summary>
    public double CircleRadiusMiles { get; set; } = 0.1;
}
