using System.Net.Http.Json;
using GPSim.Server.Configuration;
using GPSim.Shared.Models;
using Microsoft.Extensions.Options;

namespace GPSim.Server.Services;

/// <summary>
/// Implementation of webhook forwarding service with retry logic
/// </summary>
public class WebhookForwarderService : IWebhookForwarderService
{
    private readonly HttpClient _httpClient;
    private readonly WebhookSettings _settings;
    private readonly ILogger<WebhookForwarderService> _logger;

    public WebhookForwarderService(
        HttpClient httpClient,
        IOptions<WebhookSettings> settings,
        ILogger<WebhookForwarderService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> ForwardAsync(GpsPayload payload, string? webhookUrlOverride = null, CancellationToken cancellationToken = default)
    {
        var webhookUrl = webhookUrlOverride ?? _settings.DefaultUrl;
        
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("No webhook URL configured. Skipping GPS broadcast.");
            return false;
        }

        var retryCount = 0;
        var maxRetries = _settings.RetryCount;

        while (retryCount <= maxRetries)
        {
            try
            {
                _logger.LogDebug("Forwarding GPS payload to {WebhookUrl} (attempt {Attempt}/{MaxRetries})",
                    webhookUrl, retryCount + 1, maxRetries + 1);

                var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully forwarded GPS payload. Lat: {Lat}, Lng: {Lng}, Seq: {Seq}",
                        payload.Latitude, payload.Longitude, payload.SequenceNumber);
                    return true;
                }

                _logger.LogWarning("Webhook returned non-success status: {StatusCode}", response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to forward GPS payload (attempt {Attempt}/{MaxRetries})",
                    retryCount + 1, maxRetries + 1);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Webhook request timed out (attempt {Attempt}/{MaxRetries})",
                    retryCount + 1, maxRetries + 1);
            }

            retryCount++;

            if (retryCount <= maxRetries)
            {
                // Exponential backoff: 1s, 2s, 4s...
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1));
                _logger.LogDebug("Waiting {Delay}s before retry", delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogError("Failed to forward GPS payload after {MaxRetries} retries", maxRetries + 1);
        return false;
    }
}
