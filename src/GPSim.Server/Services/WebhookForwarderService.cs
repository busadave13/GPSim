using System.Diagnostics;
using System.Net.Http.Json;
using GPSim.Server.Configuration;
using GPSim.Shared.Models;
using Microsoft.Extensions.Options;

namespace GPSim.Server.Services;

/// <summary>
/// ActivitySource for webhook telemetry
/// </summary>
public static class WebhookTelemetry
{
    public static readonly ActivitySource ActivitySource = new("GPSim.Webhook", "1.0.0");
}

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
        // Resolve webhook URL: parameter override > environment variable > appsettings
        var webhookUrl = webhookUrlOverride 
            ?? Environment.GetEnvironmentVariable("GPSIM_WEBHOOK_URL")
            ?? _settings.DefaultUrl;
        
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("No webhook URL configured. Skipping GPS broadcast.");
            return false;
        }

        // Resolve default headers: environment variable > appsettings
        var defaultHeaders = Environment.GetEnvironmentVariable("GPSIM_WEBHOOK_HEADERS")
            ?? _settings.DefaultHeaders;

        // Parse headers: merge default headers with override headers (override takes precedence)
        var customHeaders = ParseHeaders(defaultHeaders);
        var overrideHeaders = ParseHeaders(webhookHeaders);
        foreach (var header in overrideHeaders)
        {
            customHeaders[header.Key] = header.Value;
        }

        // Create a custom activity/span for webhook forwarding
        using var activity = WebhookTelemetry.ActivitySource.StartActivity(
            "webhook.forward",
            ActivityKind.Client);

        // Add semantic attributes to the span
        activity?.SetTag("webhook.url", webhookUrl);
        activity?.SetTag("gps.latitude", payload.Latitude);
        activity?.SetTag("gps.longitude", payload.Longitude);
        activity?.SetTag("gps.speed", payload.Speed);
        activity?.SetTag("gps.bearing", payload.Bearing);
        activity?.SetTag("gps.timestamp", payload.Timestamp.ToString("O"));

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

            // Record response status
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetTag("webhook.success", response.IsSuccessStatusCode);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully forwarded GPS payload. Lat: {Lat}, Lng: {Lng}",
                    payload.Latitude, payload.Longitude);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return true;
            }

            _logger.LogWarning("Webhook returned non-success status: {StatusCode}", response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {(int)response.StatusCode}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to forward GPS payload");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Webhook request timed out");
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            activity?.SetTag("error.type", "Timeout");
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
