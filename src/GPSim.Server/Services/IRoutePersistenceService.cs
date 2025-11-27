using GPSim.Shared.Models;

namespace GPSim.Server.Services;

/// <summary>
/// Service for persisting and retrieving simulation routes
/// </summary>
public interface IRoutePersistenceService
{
    /// <summary>
    /// Gets all saved routes
    /// </summary>
    Task<IEnumerable<SimulationRoute>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific route by ID
    /// </summary>
    Task<SimulationRoute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new route or updates an existing one
    /// </summary>
    Task<SimulationRoute> SaveAsync(SimulationRoute route, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a route by ID
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
