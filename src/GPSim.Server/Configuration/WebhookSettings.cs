namespace GPSim.Server.Configuration;

/// <summary>
/// Webhook configuration settings
/// </summary>
public class WebhookSettings
{
    public const string SectionName = "Webhook";
    
    public string DefaultUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
}
