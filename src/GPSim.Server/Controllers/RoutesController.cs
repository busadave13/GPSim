using GPSim.Server.Services;
using GPSim.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace GPSim.Server.Controllers;

/// <summary>
/// Controller for managing simulation routes
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly IRoutePersistenceService _routeService;
    private readonly ILogger<RoutesController> _logger;

    public RoutesController(
        IRoutePersistenceService routeService,
        ILogger<RoutesController> logger)
    {
        _routeService = routeService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all saved routes
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SimulationRoute>>> GetAll(CancellationToken cancellationToken)
    {
        var routes = await _routeService.GetAllAsync(cancellationToken);
        return Ok(routes);
    }

    /// <summary>
    /// Gets a specific route by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SimulationRoute>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var route = await _routeService.GetByIdAsync(id, cancellationToken);

        if (route == null)
        {
            return NotFound();
        }

        return Ok(route);
    }

    /// <summary>
    /// Creates a new route
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SimulationRoute>> Create(
        [FromBody] SimulationRoute route,
        CancellationToken cancellationToken)
    {
        var savedRoute = await _routeService.SaveAsync(route, cancellationToken);
        _logger.LogInformation("Created route: {Id} - {Name}", savedRoute.Id, savedRoute.Name);

        return CreatedAtAction(nameof(GetById), new { id = savedRoute.Id }, savedRoute);
    }

    /// <summary>
    /// Updates an existing route
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SimulationRoute>> Update(
        Guid id,
        [FromBody] SimulationRoute route,
        CancellationToken cancellationToken)
    {
        if (id != route.Id)
        {
            return BadRequest("Route ID mismatch");
        }

        var existingRoute = await _routeService.GetByIdAsync(id, cancellationToken);
        if (existingRoute == null)
        {
            return NotFound();
        }

        var savedRoute = await _routeService.SaveAsync(route, cancellationToken);
        _logger.LogInformation("Updated route: {Id} - {Name}", savedRoute.Id, savedRoute.Name);

        return Ok(savedRoute);
    }

    /// <summary>
    /// Deletes a route
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _routeService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        _logger.LogInformation("Deleted route: {Id}", id);
        return NoContent();
    }
}
