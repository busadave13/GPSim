using System.Text.Json;
using GPSim.Server.Configuration;
using GPSim.Shared.Models;
using Microsoft.Extensions.Options;

namespace GPSim.Server.Services;

/// <summary>
/// File-based implementation of route persistence service
/// </summary>
public class RoutePersistenceService : IRoutePersistenceService
{
    private readonly string _routesDirectory;
    private readonly ILogger<RoutePersistenceService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RoutePersistenceService(
        IOptions<StorageSettings> settings,
        ILogger<RoutePersistenceService> logger)
    {
        _routesDirectory = settings.Value.RoutesDirectory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_routesDirectory))
        {
            Directory.CreateDirectory(_routesDirectory);
            _logger.LogInformation("Created routes directory: {Directory}", _routesDirectory);
        }
    }

    private string GetFilePath(Guid id) => Path.Combine(_routesDirectory, $"{id}.json");

    public async Task<IEnumerable<SimulationRoute>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var routes = new List<SimulationRoute>();

        foreach (var file in Directory.GetFiles(_routesDirectory, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var route = JsonSerializer.Deserialize<SimulationRoute>(json, _jsonOptions);
                if (route != null)
                {
                    routes.Add(route);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read route file: {File}", file);
            }
        }

        return routes.OrderByDescending(r => r.CreatedAt);
    }

    public async Task<SimulationRoute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(id);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<SimulationRoute>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read route: {Id}", id);
            return null;
        }
    }

    public async Task<SimulationRoute> SaveAsync(SimulationRoute route, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        var existingRoute = await GetByIdAsync(route.Id, cancellationToken);
        var updatedRoute = route with
        {
            LastModifiedAt = DateTime.UtcNow,
            CreatedAt = existingRoute?.CreatedAt ?? route.CreatedAt
        };

        var filePath = GetFilePath(updatedRoute.Id);
        var json = JsonSerializer.Serialize(updatedRoute, _jsonOptions);
        
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("Saved route: {Id} - {Name}", updatedRoute.Id, updatedRoute.Name);

        return updatedRoute;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(id);

        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted route: {Id}", id);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete route: {Id}", id);
            return Task.FromResult(false);
        }
    }
}
