using GPSim.Shared.Models;

namespace GPSim.Server.Services;

/// <summary>
/// Service for forwarding GPS data to external webhook endpoints
/// </summary>
public interface IWebhookForwarderService
{
    /// <summary>
    /// Forwards GPS payload to the configured webhook endpoint
    /// </summary>
    Task<bool> ForwardAsync(GpsPayload payload, string? webhookUrlOverride = null, CancellationToken cancellationToken = default);
}
