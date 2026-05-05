using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly DbConnectionFactory _factory;

    public BookingRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<BookingEntity?> GetByIdAsync(Guid bookingId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, pnr, trip_id, travel_date, user_email, seat_numbers, total_amount,
                   payment_mode, payment_status, ticket_download_url, mail_status, is_cancelled,
                   refund_amount, created_at
            FROM bookings WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", bookingId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapEntity(reader);
    }

    public async Task<IEnumerable<BookingEntity>> GetByUserEmailAsync(string userEmail)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, pnr, trip_id, travel_date, user_email, seat_numbers, total_amount,
                   payment_mode, payment_status, ticket_download_url, mail_status, is_cancelled,
                   refund_amount, created_at
            FROM bookings
            WHERE user_email = @user_email
            ORDER BY created_at DESC;", connection);
        command.Parameters.AddWithValue("user_email", userEmail);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<BookingEntity>();
        while (await reader.ReadAsync()) results.Add(MapEntity(reader));
        return results;
    }

    public async Task<BookingEntity> CreateAsync(BookingEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using var command = new NpgsqlCommand(@"
            INSERT INTO bookings
            (id, pnr, trip_id, travel_date, user_email, seat_numbers, total_amount, payment_mode, payment_status,
             ticket_download_url, mail_status, is_cancelled, refund_amount, created_at)
            VALUES
            (@id, @pnr, @trip_id, @travel_date, @user_email, @seat_numbers, @total_amount, @payment_mode, @payment_status,
             @ticket_download_url, @mail_status, FALSE, 0, @created_at);", connection, transaction);

        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("pnr", entity.Pnr);
        command.Parameters.AddWithValue("trip_id", entity.TripId);
        command.Parameters.AddWithValue("travel_date", entity.TravelDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("user_email", entity.UserEmail);
        command.Parameters.AddWithValue("seat_numbers", entity.SeatNumbers);
        command.Parameters.AddWithValue("total_amount", entity.TotalAmount);
        command.Parameters.AddWithValue("payment_mode", entity.PaymentMode);
        command.Parameters.AddWithValue("payment_status", entity.PaymentStatus);
        command.Parameters.AddWithValue("ticket_download_url", entity.TicketDownloadUrl);
        command.Parameters.AddWithValue("mail_status", entity.MailStatus);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        await command.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
        return entity;
    }

    public async Task CancelAsync(Guid bookingId, decimal refundAmount)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE bookings
            SET is_cancelled = TRUE,
                refund_amount = @refund_amount,
                payment_status = 'REFUND_INITIATED'
            WHERE id = @booking_id;", connection);
        command.Parameters.AddWithValue("refund_amount", refundAmount);
        command.Parameters.AddWithValue("booking_id", bookingId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePaymentStatusAsync(Guid bookingId, string status)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE bookings SET payment_status = @status WHERE id = @booking_id;", connection);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("booking_id", bookingId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<EnhancedBookingDetail>> GetByOperatorIdAsync(Guid operatorId, Guid? busId = null)
    {
        var sql = @"
            SELECT b.id, b.pnr, b.trip_id, b.travel_date, b.user_email, b.seat_numbers, b.total_amount,
                   b.payment_mode, b.payment_status, b.ticket_download_url, b.mail_status, b.is_cancelled,
                   b.refund_amount, b.created_at,
                   t.departure_time, t.arrival_time, t.base_price, t.platform_fee, t.trip_type,
                   r.source, r.destination, b_bus.bus_name, b_bus.bus_number, o.company_name
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            JOIN routes r ON r.id = t.route_id
            JOIN buses b_bus ON b_bus.id = t.bus_id
            JOIN operators o ON o.id = t.operator_id
            WHERE t.operator_id = @operator_id ";

        if (busId.HasValue)
        {
            sql += "AND t.bus_id = @bus_id ";
        }
        sql += "ORDER BY b.created_at DESC;";

        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        if (busId.HasValue)
        {
            command.Parameters.AddWithValue("bus_id", busId.Value);
        }

        return await ReadEnhancedDetailsAsync(command);
    }

    public async Task<HashSet<int>> GetBookedSeatNumbersAsync(Guid tripId, DateOnly travelDate)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT seat_numbers FROM bookings
            WHERE trip_id = @trip_id AND travel_date = @travel_date AND is_cancelled = FALSE;", connection);
        command.Parameters.AddWithValue("trip_id", tripId);
        command.Parameters.AddWithValue("travel_date", travelDate.ToDateTime(TimeOnly.MinValue));

        await using var reader = await command.ExecuteReaderAsync();
        var booked = new HashSet<int>();
        while (await reader.ReadAsync())
            foreach (var seat in reader.GetFieldValue<int[]>(0))
                booked.Add(seat);
        return booked;
    }

    public async Task<IEnumerable<EnhancedBookingDetail>> GetHistoryAsync(string userEmail, string? statusFilter, DateTime now)
    {
        var sql = @"
            SELECT b.id, b.pnr, b.trip_id, b.travel_date, b.user_email, b.seat_numbers, b.total_amount,
                   b.payment_mode, b.payment_status, b.ticket_download_url, b.mail_status, b.is_cancelled,
                   b.refund_amount, b.created_at,
                   t.departure_time, t.arrival_time, t.base_price, t.platform_fee, t.trip_type,
                   r.source, r.destination, b_bus.bus_name, b_bus.bus_number, o.company_name
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            JOIN routes r ON r.id = t.route_id
            JOIN buses b_bus ON b_bus.id = t.bus_id
            JOIN operators o ON o.id = t.operator_id
            WHERE b.user_email = @user_email ";

        sql += statusFilter switch
        {
            "Past" => "AND t.departure_time < @now AND b.is_cancelled = FALSE ",
            "Present" => "AND t.departure_time >= @now AND t.departure_time <= @now + INTERVAL '7 days' AND b.is_cancelled = FALSE ",
            "Future" => "AND t.departure_time > @now + INTERVAL '7 days' AND b.is_cancelled = FALSE ",
            "Cancelled" => "AND b.is_cancelled = TRUE ",
            _ => ""
        };
        sql += "ORDER BY b.created_at DESC;";

        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_email", userEmail);
        command.Parameters.AddWithValue("now", now);

        return await ReadEnhancedDetailsAsync(command);
    }

    public async Task<EnhancedBookingDetail?> GetEnhancedByIdAsync(Guid bookingId)
    {
        var sql = @"
            SELECT b.id, b.pnr, b.trip_id, b.travel_date, b.user_email, b.seat_numbers, b.total_amount,
                   b.payment_mode, b.payment_status, b.ticket_download_url, b.mail_status, b.is_cancelled,
                   b.refund_amount, b.created_at,
                   t.departure_time, t.arrival_time, t.base_price, t.platform_fee, t.trip_type,
                   r.source, r.destination, b_bus.bus_name, b_bus.bus_number, o.company_name
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            JOIN routes r ON r.id = t.route_id
            JOIN buses b_bus ON b_bus.id = t.bus_id
            JOIN operators o ON o.id = t.operator_id
            WHERE b.id = @id;";

        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", bookingId);

        var details = await ReadEnhancedDetailsAsync(command);
        return details.FirstOrDefault();
    }

    public async Task AddPassengersAsync(IEnumerable<BookingPassengerEntity> passengers)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        foreach (var p in passengers)
        {
            await using var command = new NpgsqlCommand(@"
                INSERT INTO booking_passengers (id, booking_id, seat_number, name, age, gender)
                VALUES (@id, @booking_id, @seat_number, @name, @age, @gender);", connection);
            command.Parameters.AddWithValue("id", p.Id);
            command.Parameters.AddWithValue("booking_id", p.BookingId);
            command.Parameters.AddWithValue("seat_number", p.SeatNumber);
            command.Parameters.AddWithValue("name", p.Name);
            command.Parameters.AddWithValue("age", p.Age);
            command.Parameters.AddWithValue("gender", p.Gender);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<IEnumerable<BookingPassengerEntity>> GetPassengersByBookingIdAsync(Guid bookingId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, booking_id, seat_number, name, age, gender
            FROM booking_passengers
            WHERE booking_id = @booking_id
            ORDER BY seat_number;", connection);
        command.Parameters.AddWithValue("booking_id", bookingId);
        return await ReadPassengersAsync(command);
    }

    public async Task<IEnumerable<BookingPassengerEntity>> GetPassengersByBookingIdsAsync(Guid[] bookingIds)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, booking_id, seat_number, name, age, gender
            FROM booking_passengers
            WHERE booking_id = ANY(@ids);", connection);
        command.Parameters.AddWithValue("ids", bookingIds);
        return await ReadPassengersAsync(command);
    }

    public async Task<Dictionary<int, (string Gender, string Name)>> GetBookedSeatsWithGenderAsync(Guid tripId, DateOnly travelDate)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT bp.seat_number, bp.gender, bp.name
            FROM booking_passengers bp
            JOIN bookings b ON b.id = bp.booking_id
            WHERE b.trip_id = @trip_id AND b.travel_date = @travel_date AND b.is_cancelled = FALSE;", connection);
        command.Parameters.AddWithValue("trip_id", tripId);
        command.Parameters.AddWithValue("travel_date", travelDate);

        await using var reader = await command.ExecuteReaderAsync();
        var result = new Dictionary<int, (string, string)>();
        while (await reader.ReadAsync())
            result[reader.GetInt32(0)] = (reader.GetString(1), reader.GetString(2));
        return result;
    }

    public async Task<int> GetReservedSeatCountAsync(Guid tripId, DateOnly travelDate)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        int count = 0;

        await using (var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM booking_passengers bp
            JOIN bookings b ON b.id = bp.booking_id
            WHERE b.trip_id = @trip_id AND b.travel_date = @travel_date AND b.is_cancelled = FALSE;", connection))
        {
            cmd.Parameters.AddWithValue("trip_id", tripId);
            cmd.Parameters.AddWithValue("travel_date", travelDate);
            count += Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        return count;
    }

    public async Task<IEnumerable<string>> GetAffectedUserEmailsAsync(Guid operatorId, DateTime cutoff)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT DISTINCT b.user_email
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE t.operator_id = @operator_id
              AND t.departure_time > @now
              AND b.is_cancelled = FALSE;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        command.Parameters.AddWithValue("now", cutoff);

        await using var reader = await command.ExecuteReaderAsync();
        var emails = new List<string>();
        while (await reader.ReadAsync()) emails.Add(reader.GetString(0));
        return emails;
    }

    private static async Task<List<EnhancedBookingDetail>> ReadEnhancedDetailsAsync(NpgsqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<EnhancedBookingDetail>();
        while (await reader.ReadAsync())
        {
            results.Add(new EnhancedBookingDetail
            {
                Booking = MapEntity(reader),
                Trip = new TripEntity
                {
                    DepartureTime = reader.GetDateTime(14),
                    ArrivalTime = reader.GetDateTime(15),
                    BasePrice = reader.GetDecimal(16),
                    PlatformFee = reader.GetDecimal(17),
                    TripType = reader.GetString(18)
                },
                Source = reader.GetString(19),
                Destination = reader.GetString(20),
                BusName = reader.GetString(21),
                BusNumber = reader.IsDBNull(22) ? string.Empty : reader.GetString(22),
                CompanyName = reader.GetString(23)
            });
        }
        return results;
    }

    private static async Task<List<BookingPassengerEntity>> ReadPassengersAsync(NpgsqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<BookingPassengerEntity>();
        while (await reader.ReadAsync())
        {
            results.Add(new BookingPassengerEntity
            {
                Id = reader.GetGuid(0),
                BookingId = reader.GetGuid(1),
                SeatNumber = reader.GetInt32(2),
                Name = reader.GetString(3),
                Age = reader.GetInt32(4),
                Gender = reader.GetString(5)
            });
        }
        return results;
    }

    private static BookingEntity MapEntity(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        Pnr = r.GetString(1),
        TripId = r.GetGuid(2),
        TravelDate = r.GetFieldValue<DateOnly>(3),
        UserEmail = r.GetString(4),
        SeatNumbers = r.GetFieldValue<int[]>(5),
        TotalAmount = r.GetDecimal(6),
        PaymentMode = r.GetString(7),
        PaymentStatus = r.GetString(8),
        TicketDownloadUrl = r.GetString(9),
        MailStatus = r.GetString(10),
        IsCancelled = r.GetBoolean(11),
        RefundAmount = r.GetDecimal(12),
        CreatedAt = r.GetDateTime(13)
    };
}
