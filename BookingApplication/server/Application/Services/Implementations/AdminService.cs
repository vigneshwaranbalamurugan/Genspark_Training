using server.Application.Services.Interfaces;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Models;

namespace server.Application.Services.Implementations;

public sealed class AdminService : IAdminService
{
    private readonly IOperatorRepository _operatorRepo;
    private readonly IBusRepository _busRepo;
    private readonly IRouteRepository _routeRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IPlatformFeeRepository _platformFeeRepo;
    private readonly ITripRepository _tripRepo;
    private readonly IBookingRepository _bookingRepo;

    public AdminService(
        IOperatorRepository operatorRepo,
        IBusRepository busRepo,
        IRouteRepository routeRepo,
        INotificationRepository notificationRepo,
        IPlatformFeeRepository platformFeeRepo,
        ITripRepository tripRepo,
        IBookingRepository bookingRepo)
    {
        _operatorRepo = operatorRepo;
        _busRepo = busRepo;
        _routeRepo = routeRepo;
        _notificationRepo = notificationRepo;
        _platformFeeRepo = platformFeeRepo;
        _tripRepo = tripRepo;
        _bookingRepo = bookingRepo;
    }

    public async Task<IEnumerable<OperatorResponse>> GetOperatorsAsync()
    {
        var operators = await _operatorRepo.GetAllAsync();
        return operators.Select(MapOperatorResponse);
    }

    public async Task<OperatorResponse> ApproveOperatorAsync(Guid operatorId, ApprovalRequest request)
    {
        var op = await _operatorRepo.GetByIdAsync(operatorId);
        if (op is null) throw new KeyNotFoundException("Operator not found.");

        var newStatus = request.Approve ? OperatorApprovalStatus.Approved : OperatorApprovalStatus.Rejected;
        await _operatorRepo.UpdateApprovalStatusAsync(operatorId, newStatus.ToString());

        var message = request.Approve
            ? "Your operator account has been approved. You can now register buses and routes."
            : $"Your operator account application was rejected. Reason: {request.Comment}";

        await _notificationRepo.CreateAsync(new NotificationEntity
        {
            Id = Guid.NewGuid(),
            RecipientEmail = op.Email,
            Subject = request.Approve ? "Account Approved" : "Account Rejected",
            Message = message,
            CreatedAt = DateTime.UtcNow
        });

        op.ApprovalStatus = newStatus.ToString();
        return MapOperatorResponse(op);
    }

    public async Task<OperatorResponse> DisableOperatorAsync(Guid operatorId, DisableOperatorRequest request)
    {
        var op = await _operatorRepo.GetByIdAsync(operatorId);
        if (op is null) throw new KeyNotFoundException("Operator not found.");

        await _operatorRepo.UpdateDisabledStatusAsync(operatorId, true);
        await _tripRepo.DeactivateByOperatorAsync(operatorId, DateTime.UtcNow);

        // Notify affected users
        var affectedEmails = await _bookingRepo.GetAffectedUserEmailsAsync(operatorId, DateTime.UtcNow);
        foreach (var email in affectedEmails)
        {
            await _notificationRepo.CreateAsync(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                RecipientEmail = email,
                Subject = "Trip Cancelled - Operator Disabled",
                Message = "Your upcoming trip has been cancelled because the operator is no longer active. A full refund will be processed.",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _notificationRepo.CreateAsync(new NotificationEntity
        {
            Id = Guid.NewGuid(),
            RecipientEmail = op.Email,
            Subject = "Account Disabled",
            Message = $"Your operator account has been disabled. Reason: {request.Reason}",
            CreatedAt = DateTime.UtcNow
        });

        op.IsDisabled = true;
        return MapOperatorResponse(op);
    }

    public async Task<EnableOperatorResponse> EnableOperatorAsync(Guid operatorId, EnableOperatorRequest request)
    {
        var op = await _operatorRepo.GetByIdAsync(operatorId);
        if (op is null) throw new KeyNotFoundException("Operator not found.");

        await _operatorRepo.UpdateDisabledStatusAsync(operatorId, false);

        await _notificationRepo.CreateAsync(new NotificationEntity
        {
            Id = Guid.NewGuid(),
            RecipientEmail = op.Email,
            Subject = "Account Re-enabled",
            Message = $"Your operator account has been re-enabled. Reason: {request.Reason}",
            CreatedAt = DateTime.UtcNow
        });

        return new EnableOperatorResponse
        {
            OperatorId = operatorId,
            CompanyName = op.CompanyName,
            Email = op.Email,
            ApprovalStatus = Enum.Parse<OperatorApprovalStatus>(op.ApprovalStatus),
            IsDisabled = false
        };
    }

    public async Task<RouteResponse> CreateRouteAsync(RouteRequest request)
    {
        var source = request.Source.Trim();
        var dest = request.Destination.Trim();

        if (source.Equals(dest, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Source and destination cannot be same.");

        if (await _routeRepo.ExistsAsync(source, dest))
            throw new InvalidOperationException("Route already exists.");

        var route = await _routeRepo.CreateAsync(new RouteEntity
        {
            Id = Guid.NewGuid(),
            Source = source,
            Destination = dest,
            CreatedAt = DateTime.UtcNow
        });

        return new RouteResponse { RouteId = route.Id, Source = route.Source, Destination = route.Destination };
    }

    public async Task<IEnumerable<RouteResponse>> GetRoutesAsync()
    {
        var routes = await _routeRepo.GetAllAsync();
        return routes.Select(r => new RouteResponse { RouteId = r.Id, Source = r.Source, Destination = r.Destination });
    }

    public async Task<IEnumerable<BusResponse>> GetAllBusesAsync()
    {
        var buses = await _busRepo.GetAllAsync();
        return buses.Select(MapBusResponse);
    }

    public async Task<BusResponse> ApproveBusAsync(Guid busId, ApprovalRequest request)
    {
        var bus = await _busRepo.GetByIdAsync(busId);
        if (bus is null) throw new KeyNotFoundException("Bus not found.");

        await _busRepo.UpdateApprovalAsync(busId, request.Approve);
        if (!request.Approve)
        {
            await _busRepo.DeactivateAsync(busId);
        }

        var op = await _operatorRepo.GetByIdAsync(bus.OperatorId);
        if (op is not null)
        {
            var status = request.Approve ? "approved" : "rejected";
            await _notificationRepo.CreateAsync(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                RecipientEmail = op.Email,
                Subject = $"Bus {status}",
                Message = $"Your bus '{bus.BusName}' was {status} by admin." + (!request.Approve ? $" Reason: {request.Comment}" : ""),
                CreatedAt = DateTime.UtcNow
            });
        }

        bus.IsApproved = request.Approve;
        if (!request.Approve) bus.IsActive = false;
        return MapBusResponse(bus);
    }

    public async Task<BusResponse> DisableBusAsync(Guid busId, DisableBusRequest request)
    {
        var bus = await _busRepo.GetByIdAsync(busId);
        if (bus is null) throw new KeyNotFoundException("Bus not found.");

        await _busRepo.DeactivateAsync(busId);

        var op = await _operatorRepo.GetByIdAsync(bus.OperatorId);
        if (op is not null)
        {
            await _notificationRepo.CreateAsync(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                RecipientEmail = op.Email,
                Subject = "Bus disabled",
                Message = $"Your bus '{bus.BusName}' was disabled by admin. Reason: {request.Reason}",
                CreatedAt = DateTime.UtcNow
            });
        }

        bus.IsActive = false;
        return MapBusResponse(bus);
    }

    public async Task<IEnumerable<NotificationResponse>> GetNotificationsAsync(string recipientEmail)
    {
        var items = await _notificationRepo.GetByRecipientAsync(recipientEmail.Trim().ToLowerInvariant());
        return items.Select(i => new NotificationResponse
        {
            NotificationId = i.Id,
            RecipientEmail = i.RecipientEmail,
            Subject = i.Subject,
            Message = i.Message,
            CreatedAt = i.CreatedAt
        });
    }

    public async Task<PlatformFeeResponse> SetPlatformFeeAsync(PlatformFeeRequest request)
    {
        await _platformFeeRepo.DeactivateAllAsync();
        var entity = await _platformFeeRepo.CreateAsync(new PlatformFeeEntity
        {
            Id = Guid.NewGuid(),
            Amount = request.FeeAmount,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _tripRepo.UpdatePlatformFeeForActiveTripsAsync(request.FeeAmount);

        return new PlatformFeeResponse
        {
            FeeId = entity.Id,
            Amount = entity.Amount,
            Description = entity.Description ?? string.Empty,
            UpdatedAt = entity.CreatedAt
        };
    }

    public async Task<PlatformFeeResponse> GetCurrentPlatformFeeAsync()
    {
        var active = await _platformFeeRepo.GetActiveAsync();
        if (active is null)
        {
            return new PlatformFeeResponse
            {
                FeeId = Guid.Empty,
                Amount = 0,
                Description = "No fee configured",
                UpdatedAt = DateTime.UtcNow
            };
        }

        return new PlatformFeeResponse
        {
            FeeId = active.Id,
            Amount = active.Amount,
            Description = active.Description ?? string.Empty,
            UpdatedAt = active.CreatedAt
        };
    }

    public Task<AdminRevenueResponse> GetAdminRevenueAsync()
    {
        // Placeholder: complex analytical queries should use a dedicated IAnalyticsRepository.
        return Task.FromResult(new AdminRevenueResponse
        {
            TotalPlatformFeeRevenue = 0,
            PlatformFeeRevenueThisMonth = 0,
            PlatformFeeRevenuePastMonth = 0,
            TotalBookings = 0
        });
    }

    private static OperatorResponse MapOperatorResponse(OperatorEntity op) => new()
    {
        OperatorId = op.Id,
        CompanyName = op.CompanyName,
        Email = op.Email,
        ApprovalStatus = Enum.Parse<OperatorApprovalStatus>(op.ApprovalStatus),
        IsDisabled = op.IsDisabled
    };

    private static BusResponse MapBusResponse(BusEntity b) => new()
    {
        BusId = b.Id,
        OperatorId = b.OperatorId,
        BusNumber = b.BusNumber,
        BusName = b.BusName,
        Capacity = b.Capacity,
        IsTemporarilyUnavailable = b.IsTemporarilyUnavailable,
        IsApproved = b.IsApproved,
        IsActive = b.IsActive,
        LayoutName = b.LayoutName
    };
}
