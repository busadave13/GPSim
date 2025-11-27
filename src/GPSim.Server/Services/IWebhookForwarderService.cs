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
    /// <param name="payload">The GPS payload to forward</param>
    /// <param name="webhookUrlOverride">Optional webhook URL to use instead of configured URL</param>
    /// <param name="webhookHeaders">Optional custom headers in format "Header1:Value1;Header2:Value2"</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> ForwardAsync(GpsPayload payload, string? webhookUrlOverride = null, string? webhookHeaders = null, CancellationToken cancellationToken = default);
}
