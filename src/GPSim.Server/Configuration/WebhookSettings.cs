namespace GPSim.Server.Configuration;

/// <summary>
/// Webhook configuration settings
/// </summary>
public class WebhookSettings
{
    public const string SectionName = "Webhook";
    
    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    public int RetryCount { get; set; } = 3;
}
