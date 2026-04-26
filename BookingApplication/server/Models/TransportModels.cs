using System.ComponentModel.DataAnnotations;

namespace server.Models;

public enum PaymentMode
{
    Dummy = 1,
    Razorpay = 2
}

public enum OperatorApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum TripType
{
    OneTime = 1,
    Daily = 2
}

public sealed class TripSearchRequest
{
    [Required]
    public string Source { get; set; } = string.Empty;

    [Required]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateOnly Date { get; set; }

    public DateOnly? ReturnDate { get; set; }
}

public sealed class TripSummary
{
    public Guid TripId { get; set; }
    public Guid BusId { get; set; }
    public string BusName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int Capacity { get; set; }
    public int SeatsAvailable { get; set; }
    public decimal BasePrice { get; set; }
    public decimal PlatformFee { get; set; }
    public bool IsVariablePrice { get; set; }
    public TripType TripType { get; set; } = TripType.OneTime;
    public string? DaysOfWeek { get; set; }
}

public sealed class TripSearchResponse
{
    public List<TripSummary> OutboundTrips { get; set; } = [];
    public List<TripSummary> ReturnTrips { get; set; } = [];
}

public sealed class SeatAvailabilityResponse
{
    public Guid TripId { get; set; }
    public int Capacity { get; set; }
    public int SeatsAvailableLeft { get; set; }
    public List<int> AvailableSeatNumbers { get; set; } = [];
}

public sealed class LockSeatsRequest
{
    [Required]
    public Guid TripId { get; set; }

    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<int> SeatNumbers { get; set; } = [];
}

public sealed class SeatLockResponse
{
    public Guid LockId { get; set; }
    public Guid TripId { get; set; }
    public List<int> SeatNumbers { get; set; } = [];
    public DateTime LockExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class PassengerRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 120)]
    public int Age { get; set; }

    [Required]
    [StringLength(30)]
    public string Gender { get; set; } = string.Empty;
}

public sealed class CreateBookingRequest
{
    [Required]
    public Guid LockId { get; set; }

    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    public PaymentMode PaymentMode { get; set; }

    [Required]
    [MinLength(1)]
    public List<PassengerRequest> Passengers { get; set; } = [];
}

public sealed class BookingResponse
{
    public Guid BookingId { get; set; }
    public string Pnr { get; set; } = string.Empty;
    public Guid TripId { get; set; }
    public List<int> SeatNumbers { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsCancelled { get; set; }
    public string TicketDownloadUrl { get; set; } = string.Empty;
    public string MailStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
}

public sealed class CancelBookingResponse
{
    public Guid BookingId { get; set; }
    public bool Cancelled { get; set; }
    public decimal RefundAmount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class UserProfileRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SsoProvider { get; set; }
}

public sealed class UserProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? SsoProvider { get; set; }
}

public sealed class OperatorRegisterRequest
{
    [Required]
    [StringLength(120)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class OperatorResponse
{
    public Guid OperatorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public OperatorApprovalStatus ApprovalStatus { get; set; }
    public bool IsDisabled { get; set; }
}

public sealed class RouteRequest
{
    [Required]
    [StringLength(80)]
    public string Source { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Destination { get; set; } = string.Empty;
}

public sealed class RouteResponse
{
    public Guid RouteId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
}

public sealed class BusRequest
{
    [Required]
    public Guid OperatorId { get; set; }

    [Required]
    [StringLength(80)]
    public string BusName { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Capacity { get; set; }

    [Required]
    [StringLength(50)]
    public string LayoutName { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? LayoutJson { get; set; }
}

public sealed class BusResponse
{
    public Guid BusId { get; set; }
    public Guid OperatorId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsTemporarilyUnavailable { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public string LayoutName { get; set; } = string.Empty;
}

public sealed class TripCreateRequest
{
    [Required]
    public Guid OperatorId { get; set; }

    [Required]
    public Guid BusId { get; set; }

    [Required]
    public Guid RouteId { get; set; }

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalTime { get; set; }

    [Range(0, 100000)]
    public decimal BasePrice { get; set; }

    [Range(0, 10000)]
    public decimal PlatformFee { get; set; }

    public bool IsVariablePrice { get; set; }

    [StringLength(500)]
    public string? PickupPoints { get; set; }

    [StringLength(500)]
    public string? DropPoints { get; set; }
}

public sealed class OperatorDashboardResponse
{
    public Guid OperatorId { get; set; }
    public int TotalBuses { get; set; }
    public int ActiveTrips { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
}

public sealed class ApprovalRequest
{
    [Required]
    public bool Approve { get; set; }

    [StringLength(300)]
    public string? Comment { get; set; }
}

public sealed class DisableOperatorRequest
{
    [Required]
    [StringLength(300)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class EnableOperatorRequest
{
    [Required]
    [StringLength(300)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class EnableOperatorResponse
{
    public Guid OperatorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public OperatorApprovalStatus ApprovalStatus { get; set; }
    public bool IsDisabled { get; set; }
}

public sealed class NotificationResponse
{
    public Guid NotificationId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CustomerLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string JwtToken { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class SeatWithGenderInfo
{
    public int SeatNumber { get; set; }
    public bool IsAvailable { get; set; }
    public string? BookedByGender { get; set; }
    public string? BookedByName { get; set; }
}

public sealed class SeatLayoutResponse
{
    public Guid TripId { get; set; }
    public string BusName { get; set; } = string.Empty;
    public string LayoutName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int SeatsAvailableLeft { get; set; }
    public Dictionary<int, SeatWithGenderInfo> Seats { get; set; } = [];
    public List<int> LadiesSeatsAvailable { get; set; } = [];
}

public sealed class BookingsHistoryFilter
{
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;

    public enum HistoryType { All = 0, Past = 1, Present = 2, Future = 3, Cancelled = 4 }
    public HistoryType Type { get; set; }
}

public sealed class EnhancedBookingResponse
{
    public Guid BookingId { get; set; }
    public string Pnr { get; set; } = string.Empty;
    public Guid TripId { get; set; }
    public string BusName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public List<int> SeatNumbers { get; set; } = [];
    public List<BookingPassenger> Passengers { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsCancelled { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string TicketUrl { get; set; } = string.Empty;
    public DateTime BookedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class BookingPassenger
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
}

public sealed class TicketResponse
{
    public Guid BookingId { get; set; }
    public string Pnr { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public List<BookingPassenger> Passengers { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string TicketUrl { get; set; } = string.Empty;
    public byte[]? PdfContent { get; set; }
}

public sealed class PaymentInitiateRequest
{
    [Required]
    public Guid BookingId { get; set; }

    [Required]
    public PaymentMode PaymentMode { get; set; }

    public decimal Amount { get; set; }
}

public sealed class PaymentInitiateResponse
{
    public Guid PaymentId { get; set; }
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class PaymentVerifyRequest
{
    [Required]
    public Guid PaymentId { get; set; }

    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
}

public sealed class PaymentVerifyResponse
{
    public Guid PaymentId { get; set; }
    public Guid BookingId { get; set; }
    public bool Verified { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class OperatorLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class AdminLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class AdminLoginResponse
{
    public string Email { get; set; } = string.Empty;
    public string JwtToken { get; set; } = string.Empty;
    public string Role { get; set; } = UserRole.Admin.ToString();
    public string Message { get; set; } = string.Empty;
}

public sealed class OperatorLoginResponse
{
    public Guid OperatorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JwtToken { get; set; } = string.Empty;
    public OperatorApprovalStatus ApprovalStatus { get; set; }
    public bool IsDisabled { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class BusRegistrationRequest
{
    [Required]
    [StringLength(50)]
    public string BusNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string BusName { get; set; } = string.Empty;

    [Range(10, 100)]
    public int Capacity { get; set; }

    [Required]
    [StringLength(50)]
    public string LayoutName { get; set; } = string.Empty;

    public string? LayoutJson { get; set; }
}

public sealed class BusWithNumberResponse
{
    public Guid BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string LayoutName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsTemporarilyUnavailable { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PlatformFeeRequest
{
    [Range(0, 1000)]
    public decimal FeeAmount { get; set; }

    public string? Description { get; set; }
}

public sealed class PlatformFeeResponse
{
    public Guid FeeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public sealed class OperatorBookingView
{
    public Guid BookingId { get; set; }
    public string Pnr { get; set; } = string.Empty;
    public Guid TripId { get; set; }
    public string BusName { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public List<BookingPassenger> Passengers { get; set; } = [];
    public List<int> SeatNumbers { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public bool IsCancelled { get; set; }
}

public sealed class OperatorRevenueResponse
{
    public Guid OperatorId { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenuePastMonth { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int TotalBookings { get; set; }
    public int ActiveBuses { get; set; }
    public int ActiveTrips { get; set; }
    public List<BusRevenueDetail> BusRevenue { get; set; } = [];
}

public sealed class BusRevenueDetail
{
    public Guid BusId { get; set; }
    public string BusName { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Bookings { get; set; }
}

public sealed class PreferredRouteRequest
{
    [Required]
    public Guid RouteId { get; set; }
}

public sealed class PreferredRouteResponse
{
    public Guid RouteId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
}

public sealed class PickupDropPointRequest
{
    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    public string? Address { get; set; }

    public bool IsDefault { get; set; }
}

public sealed class PickupDropPointResponse
{
    public Guid PointId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class TripCreateRequestWithDetails
{
    [Required]
    public Guid OperatorId { get; set; }

    [Required]
    public Guid BusId { get; set; }

    [Required]
    public Guid RouteId { get; set; }

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalTime { get; set; }

    [Range(1, 10000)]
    public decimal BasePrice { get; set; }

    public string? PickupPoints { get; set; }

    public string? DropPoints { get; set; }

    /// <summary>OneTime or Daily</summary>
    public TripType TripType { get; set; } = TripType.OneTime;

    /// <summary>Comma-separated days for daily trips, e.g. "Mon,Tue,Wed"</summary>
    public string? DaysOfWeek { get; set; }
}