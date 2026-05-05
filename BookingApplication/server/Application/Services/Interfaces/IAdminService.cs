using server.Models;

namespace server.Application.Services.Interfaces;

/// <summary>
/// Handles admin-specific operations: operator/bus approval, routes, platform fees, revenue.
/// </summary>
public interface IAdminService
{
    Task<IEnumerable<OperatorResponse>> GetOperatorsAsync();
    Task<OperatorResponse> ApproveOperatorAsync(Guid operatorId, ApprovalRequest request);
    Task<OperatorResponse> DisableOperatorAsync(Guid operatorId, DisableOperatorRequest request);
    Task<EnableOperatorResponse> EnableOperatorAsync(Guid operatorId, EnableOperatorRequest request);
    Task<RouteResponse> CreateRouteAsync(RouteRequest request);
    Task<IEnumerable<RouteResponse>> GetRoutesAsync();
    Task<IEnumerable<BusResponse>> GetAllBusesAsync();
    Task<BusResponse> ApproveBusAsync(Guid busId, ApprovalRequest request);
    Task<BusResponse> DisableBusAsync(Guid busId, DisableBusRequest request);
    Task<IEnumerable<NotificationResponse>> GetNotificationsAsync(string recipientEmail);
    Task<PlatformFeeResponse> SetPlatformFeeAsync(PlatformFeeRequest request);
    Task<PlatformFeeResponse> GetCurrentPlatformFeeAsync();
    Task<AdminRevenueResponse> GetAdminRevenueAsync();
}
