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

    public ConfigurationController(IOptions<MapboxSettings> mapboxSettings)
    {
        _mapboxSettings = mapboxSettings.Value;
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
    /// Gets the webhook configuration from environment variables
    /// </summary>
    [HttpGet("webhook")]
    public ActionResult<WebhookConfigResponse> GetWebhookConfig()
    {
        // Read webhook configuration from environment variables only
        var webhookUrl = Environment.GetEnvironmentVariable("GPSIM_WEBHOOK_URL") ?? string.Empty;
        var webhookHeaders = Environment.GetEnvironmentVariable("GPSIM_WEBHOOK_HEADERS") ?? string.Empty;
        
        // Parse interval from environment variable with default of 1000ms
        var intervalMs = 1000;
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

    /// <summary>
    /// Gets the phone simulation configuration from environment variables
    /// </summary>
    [HttpGet("phone")]
    public ActionResult<PhoneConfigResponse> GetPhoneConfig()
    {
        // Parse battery drain hours from environment variable with default of 1 hour
        var batteryDrainHours = 1.0;
        var batteryEnv = Environment.GetEnvironmentVariable("GPSIM_BATTERY_DRAIN_HOURS");
        if (!string.IsNullOrEmpty(batteryEnv) && double.TryParse(batteryEnv, out var parsedHours))
        {
            batteryDrainHours = parsedHours;
        }

        return Ok(new PhoneConfigResponse
        {
            BatteryDrainHours = batteryDrainHours
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

/// <summary>
/// Response model for phone simulation configuration
/// </summary>
public record PhoneConfigResponse
{
    public double BatteryDrainHours { get; init; } = 1.0;
}
