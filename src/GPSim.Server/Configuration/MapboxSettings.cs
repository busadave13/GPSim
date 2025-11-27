namespace GPSim.Server.Configuration;

/// <summary>
/// Mapbox configuration settings
/// </summary>
public class MapboxSettings
{
    public const string SectionName = "Mapbox";
    
    public string AccessToken { get; set; } = string.Empty;
}
