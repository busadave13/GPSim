using GPSim.Server.Services;
using GPSim.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace GPSim.Server.Controllers;

/// <summary>
/// Controller for forwarding GPS data to external webhooks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookForwarderService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookForwarderService webhookService,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Broadcasts GPS payload to the configured webhook endpoint
    /// </summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast(
        [FromBody] GpsPayload payload,
        [FromQuery] string? webhookUrl = null,
        [FromQuery] string? webhookHeaders = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Received GPS broadcast request: Lat={Lat}, Lng={Lng}",
            payload.Latitude, payload.Longitude);

        var success = await _webhookService.ForwardAsync(payload, webhookUrl, webhookHeaders, cancellationToken);

        if (success)
        {
            return NoContent();
        }

        return StatusCode(502, new { error = "Failed to forward GPS data to webhook" });
    }
}
