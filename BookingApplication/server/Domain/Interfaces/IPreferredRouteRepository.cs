using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for operator preferred routes and pickup/drop points.
/// </summary>
public interface IPreferredRouteRepository
{
    Task<bool> ExistsAsync(Guid operatorId, Guid routeId);
    Task CreateAsync(OperatorPreferredRouteEntity entity);
    Task<IEnumerable<RouteEntity>> GetPreferredRoutesAsync(Guid operatorId);

    // Pickup/Drop Points
    Task<PickupDropPointEntity> UpsertPointAsync(PickupDropPointEntity entity);
    Task<IEnumerable<PickupDropPointEntity>> GetPointsAsync(Guid operatorId, Guid routeId, bool isPickup);
    Task<IEnumerable<PickupDropPointEntity>> GetAllPointsByOperatorAsync(Guid operatorId);
}
