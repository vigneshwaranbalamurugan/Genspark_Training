using server.Domain.Entities;

namespace server.Domain.Interfaces;

/// <summary>
/// Data access contract for bookings and booking passengers.
/// </summary>
public interface IBookingRepository
{
    Task<BookingEntity?> GetByIdAsync(Guid bookingId);
    Task<IEnumerable<BookingEntity>> GetByUserEmailAsync(string userEmail);
    Task<BookingEntity> CreateAsync(BookingEntity entity);
    Task CancelAsync(Guid bookingId, decimal refundAmount);
    Task UpdatePaymentStatusAsync(Guid bookingId, string status);
    Task<IEnumerable<EnhancedBookingDetail>> GetByOperatorIdAsync(Guid operatorId, Guid? busId = null);

    /// <summary>Returns seat numbers reserved by confirmed bookings for a trip/date.</summary>
    Task<HashSet<int>> GetBookedSeatNumbersAsync(Guid tripId, DateOnly travelDate);

    /// <summary>Returns detailed booking + trip + route + bus info for enhanced views.</summary>
    Task<IEnumerable<EnhancedBookingDetail>> GetHistoryAsync(string userEmail, string? statusFilter, DateTime now);

    Task<EnhancedBookingDetail?> GetEnhancedByIdAsync(Guid bookingId);

    // Passengers
    Task AddPassengersAsync(IEnumerable<BookingPassengerEntity> passengers);
    Task<IEnumerable<BookingPassengerEntity>> GetPassengersByBookingIdAsync(Guid bookingId);
    Task<IEnumerable<BookingPassengerEntity>> GetPassengersByBookingIdsAsync(Guid[] bookingIds);

    /// <summary>Returns (seatNumber -> (gender, name)) for booked seats.</summary>
    Task<Dictionary<int, (string Gender, string Name)>> GetBookedSeatsWithGenderAsync(Guid tripId, DateOnly travelDate);

    /// <summary>Count of reserved seats (booked + locked) for a trip/date.</summary>
    Task<int> GetReservedSeatCountAsync(Guid tripId, DateOnly travelDate);

    /// <summary>Gets affected user emails for an operator's future trips.</summary>
    Task<IEnumerable<string>> GetAffectedUserEmailsAsync(Guid operatorId, DateTime cutoff);
}
