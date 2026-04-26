using server.Models;

namespace server.Services;

public interface ITransportService
{
    TripSearchResponse SearchTrips(TripSearchRequest request);
    SeatAvailabilityResponse GetSeatAvailability(Guid tripId);
    SeatLockResponse LockSeats(LockSeatsRequest request);
    BookingResponse CreateBooking(CreateBookingRequest request);
    CancelBookingResponse CancelBooking(Guid bookingId, string userEmail);
    IEnumerable<BookingResponse> GetBookingsByUser(string userEmail);
    UserProfileResponse UpsertUserProfile(UserProfileRequest request);
    UserProfileResponse? GetUserProfile(string email);

    OperatorResponse RegisterOperator(OperatorRegisterRequest request);
    IEnumerable<OperatorResponse> GetOperators();
    OperatorResponse ApproveOperator(Guid operatorId, ApprovalRequest request);
    OperatorResponse DisableOperator(Guid operatorId, DisableOperatorRequest request);
    EnableOperatorResponse EnableOperator(Guid operatorId, EnableOperatorRequest request);
    RouteResponse CreateRoute(RouteRequest request);
    IEnumerable<RouteResponse> GetRoutes();
    BusResponse AddBus(BusRequest request);
    IEnumerable<BusResponse> GetAllBuses();
    IEnumerable<BusResponse> GetOperatorBuses(Guid operatorId);
    BusResponse ApproveBus(Guid busId, ApprovalRequest request);
    BusResponse SetBusTemporaryAvailability(Guid operatorId, Guid busId, bool unavailable);
    void RemoveBus(Guid operatorId, Guid busId);
    TripSummary AddTrip(TripCreateRequest request);
    OperatorDashboardResponse GetOperatorDashboard(Guid operatorId);
    IEnumerable<NotificationResponse> GetNotifications(string recipientEmail);

    // Authentication
    LoginResponse Login(CustomerLoginRequest request);

    // Enhanced Search
    TripSearchResponse SearchTripsFuzzy(string source, string destination, DateOnly date, DateOnly? returnDate);

    // Seat Layout
    SeatLayoutResponse GetSeatLayout(Guid tripId);

    // Bookings History
    EnhancedBookingResponse GetEnhancedBooking(Guid bookingId, string userEmail);
    IEnumerable<EnhancedBookingResponse> GetBookingsHistory(BookingsHistoryFilter filter);

    // Tickets
    TicketResponse GetTicket(Guid bookingId, string userEmail);

    // Payments
    PaymentInitiateResponse InitiatePayment(PaymentInitiateRequest request);
    PaymentVerifyResponse VerifyPayment(PaymentVerifyRequest request);

    // Operator Features
    OperatorLoginResponse OperatorLogin(OperatorLoginRequest request);
    BusWithNumberResponse AddBusWithNumber(Guid operatorId, BusRegistrationRequest request);
    IEnumerable<OperatorBookingView> GetOperatorBookings(Guid operatorId, Guid? busId = null);
    OperatorRevenueResponse GetOperatorRevenue(Guid operatorId);
    PreferredRouteResponse AddPreferredRoute(Guid operatorId, PreferredRouteRequest request);
    IEnumerable<PreferredRouteResponse> GetOperatorPreferredRoutes(Guid operatorId);
    PickupDropPointResponse AddPickupDropPoint(Guid operatorId, Guid routeId, bool isPickup, PickupDropPointRequest request);
    IEnumerable<PickupDropPointResponse> GetPickupDropPoints(Guid operatorId, Guid routeId, bool isPickup);
    TripSummary AddTripWithDetails(Guid operatorId, TripCreateRequestWithDetails request);
    void RequestBusDisable(Guid operatorId, Guid busId, string reason);

    // Admin Features
    PlatformFeeResponse SetPlatformFee(PlatformFeeRequest request);
    PlatformFeeResponse GetCurrentPlatformFee();
}