using server.Models;

namespace server.Application.Services.Interfaces;

/// <summary>
/// Handles operator-specific operations: registration, bus/trip CRUD, revenue.
/// </summary>
public interface IOperatorService
{
    Task<OperatorResponse> RegisterOperatorAsync(OperatorRegisterRequest request);
    Task<OperatorLoginResponse> OperatorLoginAsync(OperatorLoginRequest request);
    Task<IEnumerable<BusResponse>> GetOperatorBusesAsync(Guid operatorId);
    Task<BusResponse> AddBusAsync(BusRequest request);
    Task<BusWithNumberResponse> AddBusWithNumberAsync(Guid operatorId, BusRegistrationRequest request);
    Task<BusResponse> SetBusTemporaryAvailabilityAsync(Guid operatorId, Guid busId, bool unavailable);
    Task RemoveBusAsync(Guid operatorId, Guid busId);
    Task RequestBusDisableAsync(Guid operatorId, Guid busId, string reason);
    Task<TripSummary> AddTripAsync(TripCreateRequest request);
    Task<TripSummary> AddTripWithDetailsAsync(Guid operatorId, TripCreateRequestWithDetails request);
    Task<IEnumerable<TripSummary>> GetOperatorTripsAsync(Guid operatorId);
    Task DeleteTripAsync(Guid operatorId, Guid tripId);
    Task<OperatorDashboardResponse> GetOperatorDashboardAsync(Guid operatorId);
    Task<IEnumerable<OperatorBookingView>> GetOperatorBookingsAsync(Guid operatorId, Guid? busId = null);
    Task<OperatorRevenueResponse> GetOperatorRevenueAsync(Guid operatorId);
    Task<PreferredRouteResponse> AddPreferredRouteAsync(Guid operatorId, PreferredRouteRequest request);
    Task<List<PreferredRouteResponse>> GetOperatorPreferredRoutesAsync(Guid operatorId);
    Task<PickupDropPointResponse> AddPickupDropPointAsync(Guid operatorId, Guid routeId, bool isPickup, PickupDropPointRequest request);
    Task<IEnumerable<PickupDropPointResponse>> GetPickupDropPointsAsync(Guid operatorId, Guid routeId, bool isPickup);
}
