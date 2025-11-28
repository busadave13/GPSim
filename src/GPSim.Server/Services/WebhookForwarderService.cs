using System.Net.Http.Json;
using GPSim.Server.Configuration;
using GPSim.Shared.Models;
using Microsoft.Extensions.Options;

namespace GPSim.Server.Services;

/// <summary>
/// Implementation of webhook forwarding service
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

    public async Task<bool> ForwardAsync(GpsPayload payload, string? webhookUrlOverride = null, string? webhookHeaders = null, CancellationToken cancellationToken = default)
    {
        var webhookUrl = webhookUrlOverride ?? _settings.DefaultUrl;
        
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("No webhook URL configured. Skipping GPS broadcast.");
            return false;
        }

        // Parse custom headers if provided
        var customHeaders = ParseHeaders(webhookHeaders);

        try
        {
            _logger.LogDebug("Forwarding GPS payload to {WebhookUrl}", webhookUrl);

            // Create request with custom headers
            using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
            request.Content = JsonContent.Create(payload);
            
            // Add custom headers if provided
            foreach (var header in customHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully forwarded GPS payload. Lat: {Lat}, Lng: {Lng}",
                    payload.Latitude, payload.Longitude);
                return true;
            }

            _logger.LogWarning("Webhook returned non-success status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to forward GPS payload");
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Webhook request timed out");
            return false;
        }
    }

    /// <summary>
    /// Parses custom headers from a semicolon-separated string format
    /// </summary>
    /// <param name="webhookHeaders">Headers in format "Header1:Value1;Header2:Value2"</param>
    /// <returns>Dictionary of header name-value pairs</returns>
    private static Dictionary<string, string> ParseHeaders(string? webhookHeaders)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrWhiteSpace(webhookHeaders))
        {
            return headers;
        }

        // Split by semicolon to get individual headers
        var headerPairs = webhookHeaders.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        foreach (var pair in headerPairs)
        {
            // Find the first colon to split header name and value
            var colonIndex = pair.IndexOf(':');
            if (colonIndex > 0 && colonIndex < pair.Length - 1)
            {
                var name = pair[..colonIndex].Trim();
                var value = pair[(colonIndex + 1)..].Trim();
                
                if (!string.IsNullOrEmpty(name))
                {
                    headers[name] = value;
                }
            }
        }

        return headers;
    }
}
