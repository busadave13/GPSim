namespace GPSim.Server.Configuration;

/// <summary>
/// Webhook configuration settings
/// </summary>
public class WebhookSettings
{
    public const string SectionName = "Webhook";
    
    /// <summary>
    /// Default webhook URL for GPS payload forwarding
    /// </summary>
    public string DefaultUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Default headers to include with webhook requests (format: "Header1:Value1;Header2:Value2")
    /// </summary>
    public string DefaultHeaders { get; set; } = string.Empty;
    
    /// <summary>
    /// Default interval in milliseconds between webhook sends
    /// </summary>
    public int IntervalMs { get; set; } = 1000;
    
    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    public int RetryCount { get; set; } = 3;
}
