using GPSim.Shared.Models;
using Microsoft.JSInterop;

namespace GPSim.Client.Services;

/// <summary>
/// Service for interacting with Mapbox GL JS via JavaScript interop
/// </summary>
public class MapboxInteropService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<MapboxInteropService>? _dotNetRef;
    private string? _accessToken;

    public event Func<double, double, Task>? OnMapClicked;
    public event Func<double, double, Task>? OnInitialLocationReceived;
    public event Func<double, Task>? OnZoomLevelChanged;

    public MapboxInteropService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initialize the Mapbox map
    /// </summary>
    public async Task InitializeAsync(string containerId, string accessToken, double[]? center = null, int zoom = 12, double circleRadiusMiles = 0.1)
    {
        _accessToken = accessToken;
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.initialize", containerId, accessToken, center, zoom, circleRadiusMiles);
    }

    /// <summary>
    /// Get directions between two points
    /// </summary>
    public async Task<RouteGeometry?> GetDirectionsAsync(Coordinate origin, Coordinate destination, string profile = "driving")
    {
        if (string.IsNullOrEmpty(_accessToken))
        {
            throw new InvalidOperationException("Mapbox not initialized. Call InitializeAsync first.");
        }

        var result = await _jsRuntime.InvokeAsync<DirectionsResult?>(
            "mapboxInterop.getDirections",
            _accessToken,
            new[] { origin.Longitude, origin.Latitude },
            new[] { destination.Longitude, destination.Latitude },
            profile);

        if (result == null)
        {
            return null;
        }

        return new RouteGeometry
        {
            Type = "LineString",
            Coordinates = result.Coordinates.ToList(),
            DistanceMeters = result.Distance,
            DurationSeconds = result.Duration,
            SpeedLimits = result.SpeedLimits?.ToList() ?? new List<int?>()
        };
    }

    /// <summary>
    /// Draw a route on the map
    /// </summary>
    public async Task DrawRouteAsync(RouteGeometry geometry)
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.drawRoute", (object)geometry.Coordinates.ToArray());
    }

    /// <summary>
    /// Draw a route on the map from coordinates
    /// </summary>
    public async Task DrawRouteAsync(IEnumerable<Coordinate> coordinates)
    {
        var coordArray = coordinates.Select(c => new[] { c.Longitude, c.Latitude }).ToArray();
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.drawRoute", (object)coordArray);
    }

    /// <summary>
    /// Clear the route from the map
    /// </summary>
    public async Task ClearRouteAsync()
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.clearRoute");
    }

    /// <summary>
    /// Set or update the driver marker position
    /// </summary>
    public async Task SetMarkerAsync(double longitude, double latitude, double bearing = 0)
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.setMarker", longitude, latitude, bearing);
    }

    /// <summary>
    /// Animate the marker to a new position
    /// </summary>
    public async Task AnimateMarkerAsync(double longitude, double latitude, double bearing, int durationMs = 1000)
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.animateMarker", longitude, latitude, bearing, durationMs);
    }

    /// <summary>
    /// Remove the driver marker
    /// </summary>
    public async Task RemoveMarkerAsync()
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.removeMarker");
    }

    /// <summary>
    /// Fly the map to a specific location
    /// </summary>
    public async Task FlyToAsync(double longitude, double latitude, int zoom = 14)
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.flyTo", longitude, latitude, zoom);
    }

    /// <summary>
    /// Enable click capture on the map
    /// </summary>
    public async Task EnableClickCaptureAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.enableClickCapture", _dotNetRef);
    }

    /// <summary>
    /// Disable click capture on the map
    /// </summary>
    public async Task DisableClickCaptureAsync()
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.disableClickCapture");
    }

    /// <summary>
    /// Set a waypoint marker on the map
    /// </summary>
    public async Task SetWaypointAsync(double longitude, double latitude, string type)
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.setWaypoint", longitude, latitude, type);
    }

    /// <summary>
    /// Clear all waypoint markers
    /// </summary>
    public async Task ClearWaypointsAsync()
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.clearWaypoints");
    }

    /// <summary>
    /// Interpolate position along the route
    /// </summary>
    public async Task<InterpolatedPosition?> InterpolatePositionAsync(RouteGeometry geometry, double fraction)
    {
        return await _jsRuntime.InvokeAsync<InterpolatedPosition?>(
            "mapboxInterop.interpolatePosition", 
            (object)geometry.Coordinates.ToArray(), 
            fraction);
    }

    /// <summary>
    /// Calculate total route distance in meters
    /// </summary>
    public async Task<double> CalculateRouteDistanceAsync(RouteGeometry geometry)
    {
        return await _jsRuntime.InvokeAsync<double>(
            "mapboxInterop.calculateRouteDistance", 
            (object)geometry.Coordinates.ToArray());
    }

    /// <summary>
    /// Draw a radius circle around a point
    /// </summary>
    public async Task DrawRadiusCircleAsync(double longitude, double latitude, double radiusMiles = 0.5)
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.drawRadiusCircle", longitude, latitude, radiusMiles);
    }

    /// <summary>
    /// Remove the radius circle from the map
    /// </summary>
    public async Task RemoveRadiusCircleAsync()
    {
        await _jsRuntime.InvokeVoidAsync("mapboxInterop.removeRadiusCircle");
    }

    /// <summary>
    /// Get current marker position
    /// </summary>
    public async Task<MarkerPosition?> GetMarkerPositionAsync()
    {
        return await _jsRuntime.InvokeAsync<MarkerPosition?>("mapboxInterop.getMarkerPosition");
    }

    /// <summary>
    /// Get current map zoom level
    /// </summary>
    public async Task<double> GetZoomAsync()
    {
        return await _jsRuntime.InvokeAsync<double>("mapboxInterop.getZoom");
    }

    /// <summary>
    /// JavaScript callback when map is clicked
    /// </summary>
    [JSInvokable]
    public async Task OnMapClick(double longitude, double latitude)
    {
        if (OnMapClicked != null)
        {
            await OnMapClicked.Invoke(longitude, latitude);
        }
    }

    /// <summary>
    /// JavaScript callback when initial location is detected
    /// </summary>
    [JSInvokable]
    public async Task OnInitialLocation(double longitude, double latitude)
    {
        if (OnInitialLocationReceived != null)
        {
            await OnInitialLocationReceived.Invoke(longitude, latitude);
        }
    }

    /// <summary>
    /// JavaScript callback when zoom level changes
    /// </summary>
    [JSInvokable]
    public async Task OnZoomChanged(double zoomLevel)
    {
        if (OnZoomLevelChanged != null)
        {
            await OnZoomLevelChanged.Invoke(zoomLevel);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("mapboxInterop.destroy");
        }
        catch
        {
            // Ignore disposal errors
        }
        
        _dotNetRef?.Dispose();
    }
}

/// <summary>
/// Result from Mapbox Directions API
/// </summary>
public record DirectionsResult
{
    public double[][] Coordinates { get; init; } = Array.Empty<double[]>();
    public double Distance { get; init; }
    public double Duration { get; init; }

    /// <summary>
    /// Speed limits for each segment in mph (null means unknown)
    /// </summary>
    public int?[]? SpeedLimits { get; init; }
}

/// <summary>
/// Interpolated position result
/// </summary>
public record InterpolatedPosition
{
    public double[] Position { get; init; } = Array.Empty<double>();
    public double Bearing { get; init; }

    /// <summary>
    /// Current segment index for speed limit lookup
    /// </summary>
    public int SegmentIndex { get; init; }
}

/// <summary>
/// Marker position result
/// </summary>
public record MarkerPosition
{
    public double Lng { get; init; }
    public double Lat { get; init; }
}
