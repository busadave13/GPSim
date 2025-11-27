namespace GPSim.Server.Configuration;

/// <summary>
/// Storage configuration settings
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";
    
    public string RoutesDirectory { get; set; } = "Data/Routes";
}
