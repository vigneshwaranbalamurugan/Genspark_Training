using Microsoft.AspNetCore.Identity;
using server.Application.Services.Interfaces;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;
using server.Models;

namespace server.Application.Services.Implementations;

public sealed class OperatorService : IOperatorService
{
    private readonly IOperatorRepository _operatorRepo;
    private readonly IBusRepository _busRepo;
    private readonly ITripRepository _tripRepo;
    private readonly IPreferredRouteRepository _routeRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IAuthService _authService;
    private readonly IBookingRepository _bookingRepo;
    private readonly DbConnectionFactory _factory;

    public OperatorService(
        IOperatorRepository operatorRepo,
        IBusRepository busRepo,
        ITripRepository tripRepo,
        IPreferredRouteRepository routeRepo,
        INotificationRepository notificationRepo,
        IAuthService authService,
        IBookingRepository bookingRepo,
        DbConnectionFactory factory)
    {
        _operatorRepo = operatorRepo;
        _busRepo = busRepo;
        _tripRepo = tripRepo;
        _routeRepo = routeRepo;
        _notificationRepo = notificationRepo;
        _authService = authService;
        _bookingRepo = bookingRepo;
        _factory = factory;
    }

    public async Task<OperatorResponse> RegisterOperatorAsync(OperatorRegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _operatorRepo.ExistsByEmailAsync(email))
            throw new InvalidOperationException("Operator with this email already exists.");

        var entity = new OperatorEntity
        {
            Id = Guid.NewGuid(),
            CompanyName = request.CompanyName.Trim(),
            Email = email,
            PasswordHash = new PasswordHasher<string>().HashPassword(email, request.Password),
            ApprovalStatus = OperatorApprovalStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _operatorRepo.CreateAsync(entity);

        await _notificationRepo.CreateAsync(new NotificationEntity
        {
            Id = Guid.NewGuid(),
            RecipientEmail = "admin@system.com",
            Subject = "New operator registration",
            Message = $"New operator '{saved.CompanyName}' ({saved.Email}) registered. Awaiting approval.",
            CreatedAt = DateTime.UtcNow
        });

        return new OperatorResponse
        {
            OperatorId = saved.Id,
            CompanyName = saved.CompanyName,
            Email = saved.Email,
            ApprovalStatus = OperatorApprovalStatus.Pending,
            IsDisabled = false
        };
    }

    public Task<OperatorLoginResponse> OperatorLoginAsync(OperatorLoginRequest request)
    {
        return _authService.LoginOperatorAsync(request);
    }

    public async Task<IEnumerable<BusResponse>> GetOperatorBusesAsync(Guid operatorId)
    {
        var buses = await _busRepo.GetByOperatorIdAsync(operatorId);
        return buses.Select(MapBusResponse);
    }

    public async Task<BusResponse> AddBusAsync(BusRequest request)
    {
        var entity = new BusEntity
        {
            Id = Guid.NewGuid(),
            OperatorId = request.OperatorId,
            BusName = request.BusName.Trim(),
            BusNumber = request.BusNumber,
            Capacity = request.Capacity,
            LayoutName = request.LayoutName.Trim(),
            LayoutJson = request.LayoutJson,
            IsApproved = false,
            IsTemporarilyUnavailable = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _busRepo.CreateAsync(entity);

        await _notificationRepo.CreateAsync(new NotificationEntity
        {
            Id = Guid.NewGuid(),
            RecipientEmail = "admin@system.com",
            Subject = "Bus registration pending",
            Message = $"New bus '{saved.BusName}' registered by operator. Awaiting approval.",
            CreatedAt = DateTime.UtcNow
        });

        return MapBusResponse(saved);
    }

    public async Task<BusWithNumberResponse> AddBusWithNumberAsync(Guid operatorId, BusRegistrationRequest request)
    {
        if (await _busRepo.ExistsByBusNumberAsync(request.BusNumber.Trim()))
            throw new InvalidOperationException("Bus number already exists.");

        var entity = new BusEntity
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            BusName = request.BusName.Trim(),
            BusNumber = request.BusNumber.Trim(),
            Capacity = request.Capacity,
            LayoutName = request.LayoutName.Trim(),
            LayoutJson = request.LayoutJson,
            IsApproved = false,
            IsTemporarilyUnavailable = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _busRepo.CreateAsync(entity);

        await _notificationRepo.CreateAsync(new NotificationEntity
        {
            Id = Guid.NewGuid(),
            RecipientEmail = "admin@system.com",
            Subject = "Bus registration pending",
            Message = $"New bus '{entity.BusName}' ({entity.BusNumber}) registered by operator. Awaiting approval.",
            CreatedAt = DateTime.UtcNow
        });

        return new BusWithNumberResponse
        {
            BusId = entity.Id,
            BusNumber = entity.BusNumber,
            BusName = entity.BusName,
            Capacity = entity.Capacity,
            LayoutName = entity.LayoutName,
            IsApproved = false,
            IsTemporarilyUnavailable = false,
            IsActive = true
        };
    }

    public async Task<BusResponse> SetBusTemporaryAvailabilityAsync(Guid operatorId, Guid busId, bool unavailable)
    {
        var bus = await _busRepo.GetByIdAsync(busId);
        if (bus is null || bus.OperatorId != operatorId)
            throw new KeyNotFoundException("Bus not found.");

        await _busRepo.UpdateTemporaryAvailabilityAsync(busId, unavailable);
        bus.IsTemporarilyUnavailable = unavailable;

        return MapBusResponse(bus);
    }

    public async Task RemoveBusAsync(Guid operatorId, Guid busId)
    {
        var bus = await _busRepo.GetByIdAsync(busId);
        if (bus is null || bus.OperatorId != operatorId)
            throw new KeyNotFoundException("Bus not found.");

        await _busRepo.DeactivateAsync(busId);
    }

    public async Task RequestBusDisableAsync(Guid operatorId, Guid busId, string reason)
    {
        var bus = await _busRepo.GetByIdAsync(busId);
        if (bus is null || bus.OperatorId != operatorId)
            throw new InvalidOperationException("Bus does not belong to this operator.");

        await _notificationRepo.CreateAsync(new NotificationEntity
        {
            Id = Guid.NewGuid(),
            RecipientEmail = "admin@system.com",
            Subject = "Bus disable request",
            Message = $"Operator requested to disable bus '{bus.BusName}'. Reason: {reason}",
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<TripSummary> AddTripAsync(TripCreateRequest request)
    {
        throw new NotSupportedException("Use AddTripWithDetailsAsync");
    }

    public async Task<TripSummary> AddTripWithDetailsAsync(Guid operatorId, TripCreateRequestWithDetails request)
    {
        var entity = new TripEntity
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            BusId = request.BusId,
            RouteId = request.RouteId,
            DepartureTime = request.DepartureDateTime ?? DateTime.UtcNow,
            ArrivalTime = request.ArrivalDateTime ?? DateTime.UtcNow.AddHours(5),
            BasePrice = request.BasePrice,
            PlatformFee = 0,
            IsVariablePrice = false,
            TripType = request.TripType.ToString(),
            DaysOfWeek = request.DaysOfWeek,
            IsActive = true,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DepartureTimeOnly = request.DepartureTime,
            ArrivalTimeOnly = request.ArrivalTime,
            CreatedAt = DateTime.UtcNow
        };

        await _tripRepo.CreateAsync(entity);

        return new TripSummary
        {
            TripId = entity.Id,
            BusId = entity.BusId,
            DepartureTime = entity.DepartureTime,
            ArrivalTime = entity.ArrivalTime,
            BasePrice = entity.BasePrice,
            TripType = Enum.Parse<TripType>(entity.TripType),
            DaysOfWeek = entity.DaysOfWeek
        };
    }

    public async Task<IEnumerable<TripSummary>> GetOperatorTripsAsync(Guid operatorId)
    {
        var trips = await _tripRepo.GetTripsWithDetailsByOperatorAsync(operatorId);
        return trips.Select(t => new TripSummary
        {
            TripId = t.Trip.Id,
            BusId = t.Trip.BusId,
            BusName = t.BusName,
            Source = t.Source,
            Destination = t.Destination,
            DepartureTime = t.Trip.DepartureTime,
            ArrivalTime = t.Trip.ArrivalTime,
            Capacity = t.BusCapacity,
            BasePrice = t.Trip.BasePrice,
            PlatformFee = t.Trip.PlatformFee,
            IsActive = t.Trip.IsActive,
            TripType = Enum.Parse<TripType>(t.Trip.TripType),
            DaysOfWeek = t.Trip.DaysOfWeek,
            StartDate = t.Trip.StartDate,
            EndDate = t.Trip.EndDate,
            ArrivalDayOffset = t.Trip.ArrivalDayOffset
        });
    }

    public async Task DeleteTripAsync(Guid operatorId, Guid tripId)
    {
        await _tripRepo.DeactivateAsync(tripId, operatorId);
    }

    public async Task<OperatorDashboardResponse> GetOperatorDashboardAsync(Guid operatorId)
    {
        var buses = await _busRepo.GetByOperatorIdAsync(operatorId);
        var activeBuses = buses.Count(b => b.IsActive && b.IsApproved);
        var activeTrips = await _tripRepo.CountActiveByOperatorAsync(operatorId);

        return new OperatorDashboardResponse
        {
            OperatorId = operatorId,
            TotalBuses = activeBuses,
            ActiveTrips = activeTrips,
            TotalBookings = 0,
            TotalRevenue = 0
        };
    }

    public async Task<IEnumerable<OperatorBookingView>> GetOperatorBookingsAsync(Guid operatorId, Guid? busId = null)
    {
        var bookings = await _bookingRepo.GetByOperatorIdAsync(operatorId, busId);
        return bookings.Select(b => new OperatorBookingView
        {
            BookingId = b.Booking.Id,
            Pnr = b.Booking.Pnr,
            TripId = b.Booking.TripId,
            BusName = b.BusName,
            BusNumber = b.BusNumber,
            Route = $"{b.Source} → {b.Destination}",
            DepartureTime = b.Trip.DepartureTime,
            SeatNumbers = b.Booking.SeatNumbers.ToList(),
            TotalAmount = b.Booking.TotalAmount,
            PaymentStatus = b.Booking.PaymentStatus,
            IsCancelled = b.Booking.IsCancelled
        });
    }

    public async Task<OperatorRevenueResponse> GetOperatorRevenueAsync(Guid operatorId)
    {
        var bookings = await _bookingRepo.GetByOperatorIdAsync(operatorId);
        var confirmed = bookings.Where(b => !b.Booking.IsCancelled && b.Booking.PaymentStatus == "PAID").ToList();
        var buses = await _busRepo.GetByOperatorIdAsync(operatorId);
        var activeTrips = await _tripRepo.CountActiveByOperatorAsync(operatorId);

        return new OperatorRevenueResponse
        {
            OperatorId = operatorId,
            TotalRevenue = confirmed.Sum(b => b.Booking.TotalAmount),
            RevenuePastMonth = 0,
            RevenueThisMonth = 0,
            TotalBookings = confirmed.Count,
            ActiveBuses = buses.Count(b => b.IsActive && b.IsApproved),
            ActiveTrips = activeTrips,
            BusRevenue = new List<BusRevenueDetail>()
        };
    }

    public async Task<PreferredRouteResponse> AddPreferredRouteAsync(Guid operatorId, PreferredRouteRequest request)
    {
        await _routeRepo.CreateAsync(new OperatorPreferredRouteEntity
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            RouteId = request.RouteId,
            CreatedAt = DateTime.UtcNow
        });

        return new PreferredRouteResponse { RouteId = request.RouteId };
    }

    public async Task<List<PreferredRouteResponse>> GetOperatorPreferredRoutesAsync(Guid operatorId)
    {
        var routes = await _routeRepo.GetPreferredRoutesAsync(operatorId);
        return routes.Select(r => new PreferredRouteResponse { RouteId = r.Id, Source = r.Source, Destination = r.Destination }).ToList();
    }

    public async Task<PickupDropPointResponse> AddPickupDropPointAsync(Guid operatorId, Guid routeId, bool isPickup, PickupDropPointRequest request)
    {
        var entity = await _routeRepo.UpsertPointAsync(new PickupDropPointEntity
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            RouteId = routeId,
            IsPickup = isPickup,
            Location = request.Location,
            Address = request.Address,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        });

        return new PickupDropPointResponse
        {
            PointId = entity.Id,
            Location = entity.Location,
            Address = entity.Address,
            IsDefault = entity.IsDefault,
            IsPickup = entity.IsPickup
        };
    }

    public async Task<IEnumerable<PickupDropPointResponse>> GetPickupDropPointsAsync(Guid operatorId, Guid routeId, bool isPickup)
    {
        var points = await _routeRepo.GetPointsAsync(operatorId, routeId, isPickup);
        return points.Select(p => new PickupDropPointResponse
        {
            PointId = p.Id,
            Location = p.Location,
            Address = p.Address,
            IsDefault = p.IsDefault,
            IsPickup = p.IsPickup
        });
    }

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
