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
            AccessToken = _mapboxSettings.AccessToken
        });
    }
}

/// <summary>
/// Response model for Mapbox configuration
/// </summary>
public record MapboxConfigResponse
{
    public string AccessToken { get; init; } = string.Empty;
}
