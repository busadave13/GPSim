using GPSim.Server.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GPSim.Server.Controllers;

/// <summary>
/// Controller for retrieving application configuration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly MapboxSettings _mapboxSettings;
    private readonly WebhookSettings _webhookSettings;

    public ConfigurationController(
        IOptions<MapboxSettings> mapboxSettings,
        IOptions<WebhookSettings> webhookSettings)
    {
        _mapboxSettings = mapboxSettings.Value;
        _webhookSettings = webhookSettings.Value;
    }

    /// <summary>
    /// Gets the Mapbox configuration for the client
    /// </summary>
    [HttpGet("mapbox")]
    public ActionResult<MapboxConfigResponse> GetMapboxConfig()
    {
        return Ok(new MapboxConfigResponse
        {
            AccessToken = _mapboxSettings.AccessToken,
            CircleRadiusMiles = _mapboxSettings.CircleRadiusMiles
        });
    }

    /// <summary>
    /// Gets the resolved webhook configuration (env vars take precedence over appsettings)
    /// </summary>
    [HttpGet("webhook")]
    public ActionResult<WebhookConfigResponse> GetWebhookConfig()
    {
        // Resolve from environment variables with fallback to appsettings
        var webhookUrl = Environment.GetEnvironmentVariable("GPSIM_WEBHOOK_URL")
            ?? _webhookSettings.DefaultUrl;
        var webhookHeaders = Environment.GetEnvironmentVariable("GPSIM_WEBHOOK_HEADERS")
            ?? _webhookSettings.DefaultHeaders;
        
        // Parse interval from environment variable
        var intervalMs = _webhookSettings.IntervalMs;
        var intervalEnv = Environment.GetEnvironmentVariable("GPSIM_WEBHOOK_INTERVAL_MS");
        if (!string.IsNullOrEmpty(intervalEnv) && int.TryParse(intervalEnv, out var parsedInterval))
        {
            intervalMs = parsedInterval;
        }

        return Ok(new WebhookConfigResponse
        {
            DefaultUrl = webhookUrl,
            DefaultHeaders = webhookHeaders,
            IntervalMs = intervalMs
        });
    }
}

/// <summary>
/// Response model for Mapbox configuration
/// </summary>
public record MapboxConfigResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public double CircleRadiusMiles { get; init; } = 0.1;
}

/// <summary>
/// Response model for webhook configuration
/// </summary>
public record WebhookConfigResponse
{
    public string DefaultUrl { get; init; } = string.Empty;
    public string DefaultHeaders { get; init; } = string.Empty;
    public int IntervalMs { get; init; } = 1000;
}
