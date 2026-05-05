using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for routes.
/// </summary>
public interface IRouteRepository
{
    Task<RouteEntity?> GetByIdAsync(Guid routeId);
    Task<IEnumerable<RouteEntity>> GetAllAsync();
    Task<RouteEntity> CreateAsync(RouteEntity entity);
    Task<bool> ExistsAsync(string source, string destination);
}
