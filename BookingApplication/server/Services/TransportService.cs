using Microsoft.AspNetCore.Identity;
using Npgsql;
using server.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace server.Services;

public sealed class TransportService : ITransportService
{
    private static readonly TimeSpan SeatLockDuration = TimeSpan.FromMinutes(5);
    private readonly string connectionString;
    private readonly PasswordHasher<string> passwordHasher = new();

    public TransportService(string connectionString)
    {
        this.connectionString = connectionString;
        EnsureSchema();
        SeedDemoData();
    }

    public TripSearchResponse SearchTrips(TripSearchRequest request)
    {
        DeleteExpiredLocks();

        var outbound = FindTrips(request.Source, request.Destination, request.Date);
        var response = new TripSearchResponse { OutboundTrips = outbound };

        if (request.ReturnDate.HasValue)
        {
            response.ReturnTrips = FindTrips(request.Destination, request.Source, request.ReturnDate.Value);
        }

        return response;
    }

    public SeatAvailabilityResponse GetSeatAvailability(Guid tripId, DateOnly travelDate)
    {
        DeleteExpiredLocks();

        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT t.id, b.capacity
            FROM trips t
            JOIN buses b ON b.id = t.bus_id
            WHERE t.id = @trip_id AND t.is_active = TRUE;", connection);
        command.Parameters.AddWithValue("trip_id", tripId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Trip not found.");
        }

        var capacity = reader.GetInt32(reader.GetOrdinal("capacity"));
        reader.Close();

        var reservedSeats = GetReservedSeatNumbers(connection, tripId, travelDate);
        var availableSeats = Enumerable.Range(1, capacity).Where(seat => !reservedSeats.Contains(seat)).ToList();

        return new SeatAvailabilityResponse
        {
            TripId = tripId,
            Capacity = capacity,
            SeatsAvailableLeft = availableSeats.Count,
            AvailableSeatNumbers = availableSeats
        };
    }

    public SeatLockResponse LockSeats(LockSeatsRequest request)
    {
        DeleteExpiredLocks();

        if (request.SeatNumbers.Count != request.SeatNumbers.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate seat numbers are not allowed.");
        }

        using var connection = OpenConnection();
        var availability = GetSeatAvailability(request.TripId, request.TravelDate);
        var notAvailable = request.SeatNumbers.Where(seat => !availability.AvailableSeatNumbers.Contains(seat)).ToList();
        if (notAvailable.Count > 0)
        {
            throw new InvalidOperationException($"Seats not available: {string.Join(",", notAvailable)}");
        }

        var lockId = Guid.NewGuid();
        var lockExpiresAt = DateTime.UtcNow.Add(SeatLockDuration);

        using var command = new NpgsqlCommand(@"
            INSERT INTO seat_locks (id, trip_id, travel_date, user_email, seat_numbers, expires_at, created_at)
            VALUES (@id, @trip_id, @travel_date, @user_email, @seat_numbers, @expires_at, @created_at);", connection);

        command.Parameters.AddWithValue("id", lockId);
        command.Parameters.AddWithValue("trip_id", request.TripId);
        command.Parameters.AddWithValue("travel_date", request.TravelDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("user_email", NormalizeEmail(request.UserEmail));
        command.Parameters.AddWithValue("seat_numbers", request.SeatNumbers.ToArray());
        command.Parameters.AddWithValue("expires_at", lockExpiresAt);
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.ExecuteNonQuery();

        return new SeatLockResponse
        {
            LockId = lockId,
            TripId = request.TripId,
            SeatNumbers = request.SeatNumbers,
            LockExpiresAt = lockExpiresAt,
            Message = "Seat lock created for 5 minutes."
        };
    }

    public BookingResponse CreateBooking(CreateBookingRequest request)
    {
        DeleteExpiredLocks();

        using var connection = OpenConnection();
        using var lockCommand = new NpgsqlCommand(@"
            SELECT id, trip_id, user_email, seat_numbers, expires_at
            FROM seat_locks
            WHERE id = @lock_id;", connection);
        lockCommand.Parameters.AddWithValue("lock_id", request.LockId);

        using var lockReader = lockCommand.ExecuteReader();
        if (!lockReader.Read())
        {
            throw new KeyNotFoundException("Seat lock not found.");
        }

        var lockExpiresAt = lockReader.GetDateTime(lockReader.GetOrdinal("expires_at"));
        if (lockExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Seat lock expired. Please lock seats again.");
        }

        var lockedEmail = lockReader.GetString(lockReader.GetOrdinal("user_email"));
        var userEmail = NormalizeEmail(request.UserEmail);
        if (!string.Equals(lockedEmail, userEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Seat lock belongs to another user.");
        }

        var tripId = lockReader.GetGuid(lockReader.GetOrdinal("trip_id"));
        var seatNumbers = lockReader.GetFieldValue<int[]>(lockReader.GetOrdinal("seat_numbers")).ToList();
        lockReader.Close();

        if (request.Passengers.Count != seatNumbers.Count)
        {
            throw new InvalidOperationException("Passenger count must match locked seat count.");
        }

        using var priceCommand = new NpgsqlCommand(@"
            SELECT base_price, platform_fee, is_variable_price
            FROM trips
            WHERE id = @trip_id;", connection);
        priceCommand.Parameters.AddWithValue("trip_id", tripId);

        using var priceReader = priceCommand.ExecuteReader();
        if (!priceReader.Read())
        {
            throw new KeyNotFoundException("Trip not found.");
        }

        var basePrice = priceReader.GetDecimal(priceReader.GetOrdinal("base_price"));
        var platformFee = priceReader.GetDecimal(priceReader.GetOrdinal("platform_fee"));
        var isVariable = priceReader.GetBoolean(priceReader.GetOrdinal("is_variable_price"));
        priceReader.Close();

        var variableMultiplier = isVariable ? 1.10m : 1.00m;
        var totalAmount = (basePrice * variableMultiplier + platformFee) * seatNumbers.Count;
        var paymentStatus = request.PaymentMode == PaymentMode.Dummy ? "PAID_DUMMY" : "PAID_RAZORPAY_SIMULATED";

        var bookingId = Guid.NewGuid();
        var pnr = $"BT{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
        var ticketUrl = $"/api/bookings/{bookingId}/ticket/download";

        using var transaction = connection.BeginTransaction();

        using (var bookingCommand = new NpgsqlCommand(@"
            INSERT INTO bookings
            (id, pnr, trip_id, travel_date, user_email, seat_numbers, total_amount, payment_mode, payment_status,
             ticket_download_url, mail_status, is_cancelled, refund_amount, created_at)
            VALUES
            (@id, @pnr, @trip_id, @travel_date, @user_email, @seat_numbers, @total_amount, @payment_mode, @payment_status,
             @ticket_download_url, @mail_status, FALSE, 0, @created_at);", connection, transaction))
        {
            bookingCommand.Parameters.AddWithValue("id", bookingId);
            bookingCommand.Parameters.AddWithValue("pnr", pnr);
            bookingCommand.Parameters.AddWithValue("trip_id", tripId);
            bookingCommand.Parameters.AddWithValue("travel_date", request.TravelDate.ToDateTime(TimeOnly.MinValue));
            bookingCommand.Parameters.AddWithValue("user_email", userEmail);
            bookingCommand.Parameters.AddWithValue("seat_numbers", seatNumbers.ToArray());
            bookingCommand.Parameters.AddWithValue("total_amount", totalAmount);
            bookingCommand.Parameters.AddWithValue("payment_mode", request.PaymentMode.ToString());
            bookingCommand.Parameters.AddWithValue("payment_status", paymentStatus);
            bookingCommand.Parameters.AddWithValue("ticket_download_url", ticketUrl);
            bookingCommand.Parameters.AddWithValue("mail_status", "MAIL_QUEUED");
            bookingCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            bookingCommand.ExecuteNonQuery();
        }

        for (var index = 0; index < request.Passengers.Count; index++)
        {
            var passenger = request.Passengers[index];
            using var passengerCommand = new NpgsqlCommand(@"
                INSERT INTO booking_passengers (id, booking_id, seat_number, name, age, gender)
                VALUES (@id, @booking_id, @seat_number, @name, @age, @gender);", connection, transaction);
            passengerCommand.Parameters.AddWithValue("id", Guid.NewGuid());
            passengerCommand.Parameters.AddWithValue("booking_id", bookingId);
            passengerCommand.Parameters.AddWithValue("seat_number", seatNumbers[index]);
            passengerCommand.Parameters.AddWithValue("name", passenger.Name.Trim());
            passengerCommand.Parameters.AddWithValue("age", passenger.Age);
            passengerCommand.Parameters.AddWithValue("gender", passenger.Gender.Trim());
            passengerCommand.ExecuteNonQuery();
        }

        using (var deleteLockCommand = new NpgsqlCommand("DELETE FROM seat_locks WHERE id = @lock_id;", connection, transaction))
        {
            deleteLockCommand.Parameters.AddWithValue("lock_id", request.LockId);
            deleteLockCommand.ExecuteNonQuery();
        }

        CreateNotification(connection, transaction, userEmail, "Ticket confirmation", $"Booking {pnr} confirmed. Ticket: {ticketUrl}");

        transaction.Commit();

        return new BookingResponse
        {
            BookingId = bookingId,
            Pnr = pnr,
            TripId = tripId,
            SeatNumbers = seatNumbers,
            TotalAmount = totalAmount,
            RefundAmount = 0,
            IsCancelled = false,
            TicketDownloadUrl = ticketUrl,
            MailStatus = "MAIL_QUEUED",
            PaymentStatus = paymentStatus
        };
    }

    public CancelBookingResponse CancelBooking(Guid bookingId, string userEmail)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT b.id, b.user_email, b.total_amount, b.is_cancelled, t.departure_time
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE b.id = @booking_id;", connection);
        command.Parameters.AddWithValue("booking_id", bookingId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        var bookingEmail = reader.GetString(reader.GetOrdinal("user_email"));
        if (!string.Equals(bookingEmail, NormalizeEmail(userEmail), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Booking does not belong to the requested user.");
        }

        if (reader.GetBoolean(reader.GetOrdinal("is_cancelled")))
        {
            throw new InvalidOperationException("Booking already cancelled.");
        }

        var departure = reader.GetDateTime(reader.GetOrdinal("departure_time"));
        if (departure - DateTime.UtcNow < TimeSpan.FromHours(12))
        {
            throw new InvalidOperationException("Cancellation is allowed only before 12 hours of departure.");
        }

        var totalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount"));
        reader.Close();

        var refundAmount = Math.Round(totalAmount * 0.20m, 2, MidpointRounding.AwayFromZero);

        using var transaction = connection.BeginTransaction();
        using (var updateCommand = new NpgsqlCommand(@"
            UPDATE bookings
            SET is_cancelled = TRUE,
                refund_amount = @refund_amount,
                payment_status = 'REFUND_INITIATED'
            WHERE id = @booking_id;", connection, transaction))
        {
            updateCommand.Parameters.AddWithValue("refund_amount", refundAmount);
            updateCommand.Parameters.AddWithValue("booking_id", bookingId);
            updateCommand.ExecuteNonQuery();
        }

        CreateNotification(connection, transaction, NormalizeEmail(userEmail), "Booking cancelled", $"Booking cancelled. Refund amount: {refundAmount}");
        transaction.Commit();

        return new CancelBookingResponse
        {
            BookingId = bookingId,
            Cancelled = true,
            RefundAmount = refundAmount,
            Message = "Booking cancelled. 20% refund initiated."
        };
    }

    public IEnumerable<BookingResponse> GetBookingsByUser(string userEmail)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, pnr, trip_id, seat_numbers, total_amount, refund_amount,
                   is_cancelled, ticket_download_url, mail_status, payment_status
            FROM bookings
            WHERE user_email = @user_email
            ORDER BY created_at DESC;", connection);
        command.Parameters.AddWithValue("user_email", NormalizeEmail(userEmail));

        using var reader = command.ExecuteReader();
        var items = new List<BookingResponse>();
        while (reader.Read())
        {
            items.Add(new BookingResponse
            {
                BookingId = reader.GetGuid(reader.GetOrdinal("id")),
                Pnr = reader.GetString(reader.GetOrdinal("pnr")),
                TripId = reader.GetGuid(reader.GetOrdinal("trip_id")),
                SeatNumbers = reader.GetFieldValue<int[]>(reader.GetOrdinal("seat_numbers")).ToList(),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                RefundAmount = reader.GetDecimal(reader.GetOrdinal("refund_amount")),
                IsCancelled = reader.GetBoolean(reader.GetOrdinal("is_cancelled")),
                TicketDownloadUrl = $"/api/bookings/{reader.GetGuid(reader.GetOrdinal("id"))}/ticket/download",
                MailStatus = reader.GetString(reader.GetOrdinal("mail_status")),
                PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status"))
            });
        }

        return items;
    }

    public UserProfileResponse UpsertUserProfile(UserProfileRequest request)
    {
        var email = NormalizeEmail(request.Email);
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            INSERT INTO users (id, email, full_name, sso_provider, created_at, updated_at)
            VALUES (@id, @email, @full_name, @sso_provider, @created_at, @updated_at)
            ON CONFLICT (email)
            DO UPDATE SET
                full_name = EXCLUDED.full_name,
                sso_provider = EXCLUDED.sso_provider,
                updated_at = EXCLUDED.updated_at
            RETURNING id, email, full_name, sso_provider;", connection);

        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("full_name", request.FullName.Trim());
        command.Parameters.AddWithValue("sso_provider", request.SsoProvider ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("updated_at", DateTime.UtcNow);

        using var reader = command.ExecuteReader();
        reader.Read();

        return new UserProfileResponse
        {
            UserId = reader.GetGuid(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            SsoProvider = reader.IsDBNull(reader.GetOrdinal("sso_provider")) ? null : reader.GetString(reader.GetOrdinal("sso_provider"))
        };
    }

    public UserProfileResponse? GetUserProfile(string email)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, email, full_name, sso_provider
            FROM users
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("email", NormalizeEmail(email));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new UserProfileResponse
        {
            UserId = reader.GetGuid(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            SsoProvider = reader.IsDBNull(reader.GetOrdinal("sso_provider")) ? null : reader.GetString(reader.GetOrdinal("sso_provider"))
        };
    }

    public OperatorResponse RegisterOperator(OperatorRegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);
        using var connection = OpenConnection();

        using (var existsCommand = new NpgsqlCommand("SELECT 1 FROM operators WHERE email = @email;", connection))
        {
            existsCommand.Parameters.AddWithValue("email", email);
            var exists = existsCommand.ExecuteScalar();
            if (exists is not null)
            {
                throw new InvalidOperationException("Operator already registered.");
            }
        }

        var operatorId = Guid.NewGuid();
        var passwordHash = passwordHasher.HashPassword(email, request.Password);

        using (var insertCommand = new NpgsqlCommand(@"
            INSERT INTO operators (id, company_name, email, password_hash, approval_status, is_disabled, created_at)
            VALUES (@id, @company_name, @email, @password_hash, @approval_status, FALSE, @created_at);", connection))
        {
            insertCommand.Parameters.AddWithValue("id", operatorId);
            insertCommand.Parameters.AddWithValue("company_name", request.CompanyName.Trim());
            insertCommand.Parameters.AddWithValue("email", email);
            insertCommand.Parameters.AddWithValue("password_hash", passwordHash);
            insertCommand.Parameters.AddWithValue("approval_status", OperatorApprovalStatus.Pending.ToString());
            insertCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            insertCommand.ExecuteNonQuery();
        }

        CreateNotification(connection, null, email, "Operator registration", "Registration submitted. Awaiting admin approval.");

        return new OperatorResponse
        {
            OperatorId = operatorId,
            CompanyName = request.CompanyName.Trim(),
            Email = email,
            ApprovalStatus = OperatorApprovalStatus.Pending,
            IsDisabled = false
        };
    }

    public IEnumerable<OperatorResponse> GetOperators()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, company_name, email, approval_status, is_disabled
            FROM operators
            ORDER BY created_at DESC;", connection);

        using var reader = command.ExecuteReader();
        var items = new List<OperatorResponse>();
        while (reader.Read())
        {
            items.Add(new OperatorResponse
            {
                OperatorId = reader.GetGuid(reader.GetOrdinal("id")),
                CompanyName = reader.GetString(reader.GetOrdinal("company_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                ApprovalStatus = ParseApprovalStatus(reader.GetString(reader.GetOrdinal("approval_status"))),
                IsDisabled = reader.GetBoolean(reader.GetOrdinal("is_disabled"))
            });
        }

        return items;
    }

    public OperatorResponse ApproveOperator(Guid operatorId, ApprovalRequest request)
    {
        using var connection = OpenConnection();
        var operatorData = GetOperator(connection, operatorId);
        var status = request.Approve ? OperatorApprovalStatus.Approved : OperatorApprovalStatus.Rejected;

        using (var updateCommand = new NpgsqlCommand(@"
            UPDATE operators
            SET approval_status = @approval_status
            WHERE id = @id;", connection))
        {
            updateCommand.Parameters.AddWithValue("approval_status", status.ToString());
            updateCommand.Parameters.AddWithValue("id", operatorId);
            updateCommand.ExecuteNonQuery();
        }

        var note = request.Approve ? "approved" : "rejected";
        CreateNotification(connection, null, operatorData.Email, "Operator approval update", $"Your account has been {note}. {request.Comment}");

        return new OperatorResponse
        {
            OperatorId = operatorData.Id,
            CompanyName = operatorData.CompanyName,
            Email = operatorData.Email,
            ApprovalStatus = status,
            IsDisabled = operatorData.IsDisabled
        };
    }

    public OperatorResponse DisableOperator(Guid operatorId, DisableOperatorRequest request)
    {
        using var connection = OpenConnection();
        var operatorData = GetOperator(connection, operatorId);

        using var transaction = connection.BeginTransaction();
        using (var updateOperator = new NpgsqlCommand(@"
            UPDATE operators
            SET is_disabled = TRUE
            WHERE id = @id;", connection, transaction))
        {
            updateOperator.Parameters.AddWithValue("id", operatorId);
            updateOperator.ExecuteNonQuery();
        }

        using (var deactivateTrips = new NpgsqlCommand(@"
            UPDATE trips
            SET is_active = FALSE
            WHERE operator_id = @operator_id AND departure_time > @now;", connection, transaction))
        {
            deactivateTrips.Parameters.AddWithValue("operator_id", operatorId);
            deactivateTrips.Parameters.AddWithValue("now", DateTime.UtcNow);
            deactivateTrips.ExecuteNonQuery();
        }

        CreateNotification(connection, transaction, operatorData.Email, "Operator disabled", $"Admin disabled your operator account. Reason: {request.Reason}");

        using (var usersCommand = new NpgsqlCommand(@"
            SELECT DISTINCT b.user_email
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE t.operator_id = @operator_id
              AND t.departure_time > @now
              AND b.is_cancelled = FALSE;", connection, transaction))
        {
            usersCommand.Parameters.AddWithValue("operator_id", operatorId);
            usersCommand.Parameters.AddWithValue("now", DateTime.UtcNow);

            using var reader = usersCommand.ExecuteReader();
            var recipients = new List<string>();
            while (reader.Read())
            {
                recipients.Add(reader.GetString(0));
            }

            reader.Close();
            foreach (var recipient in recipients)
            {
                CreateNotification(connection, transaction, recipient, "Service impact notice", $"An operator was disabled by admin. Your trip may be affected. Reason: {request.Reason}");
            }
        }

        transaction.Commit();

        return new OperatorResponse
        {
            OperatorId = operatorData.Id,
            CompanyName = operatorData.CompanyName,
            Email = operatorData.Email,
            ApprovalStatus = ParseApprovalStatus(operatorData.ApprovalStatus),
            IsDisabled = true
        };
    }

    public EnableOperatorResponse EnableOperator(Guid operatorId, EnableOperatorRequest request)
    {
        using var connection = OpenConnection();
        var operatorData = GetOperator(connection, operatorId);

        using var updateCommand = new NpgsqlCommand(@"
            UPDATE operators
            SET is_disabled = FALSE
            WHERE id = @id;", connection);
        updateCommand.Parameters.AddWithValue("id", operatorId);
        updateCommand.ExecuteNonQuery();

        CreateNotification(connection, null, operatorData.Email, "Operator enabled", $"Admin enabled your operator account. Reason: {request.Reason}");

        return new EnableOperatorResponse
        {
            OperatorId = operatorData.Id,
            CompanyName = operatorData.CompanyName,
            Email = operatorData.Email,
            ApprovalStatus = ParseApprovalStatus(operatorData.ApprovalStatus),
            IsDisabled = false
        };
    }

    public RouteResponse CreateRoute(RouteRequest request)
    {
        using var connection = OpenConnection();
        using var exists = new NpgsqlCommand(@"
            SELECT id FROM routes WHERE source = @source AND destination = @destination;", connection);
        exists.Parameters.AddWithValue("source", request.Source.Trim());
        exists.Parameters.AddWithValue("destination", request.Destination.Trim());
        var existing = exists.ExecuteScalar();
        if (existing is not null)
        {
            throw new InvalidOperationException("Route already exists.");
        }

        var routeId = Guid.NewGuid();
        using var insert = new NpgsqlCommand(@"
            INSERT INTO routes (id, source, destination, created_at)
            VALUES (@id, @source, @destination, @created_at);", connection);
        insert.Parameters.AddWithValue("id", routeId);
        insert.Parameters.AddWithValue("source", request.Source.Trim());
        insert.Parameters.AddWithValue("destination", request.Destination.Trim());
        insert.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        insert.ExecuteNonQuery();

        return new RouteResponse { RouteId = routeId, Source = request.Source.Trim(), Destination = request.Destination.Trim() };
    }

    public IEnumerable<RouteResponse> GetRoutes()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand("SELECT id, source, destination FROM routes ORDER BY source, destination;", connection);
        using var reader = command.ExecuteReader();
        var items = new List<RouteResponse>();
        while (reader.Read())
        {
            items.Add(new RouteResponse
            {
                RouteId = reader.GetGuid(0),
                Source = reader.GetString(1),
                Destination = reader.GetString(2)
            });
        }

        return items;
    }

    public BusResponse AddBus(BusRequest request)
    {
        using var connection = OpenConnection();
        var operatorData = GetOperator(connection, request.OperatorId);
        if (operatorData.IsDisabled || ParseApprovalStatus(operatorData.ApprovalStatus) != OperatorApprovalStatus.Approved)
        {
            throw new InvalidOperationException("Operator must be approved and active before adding buses.");
        }

        var busId = Guid.NewGuid();
        using var command = new NpgsqlCommand(@"
            INSERT INTO buses
            (id, operator_id, bus_name, bus_number, capacity, layout_name, layout_json, is_temporarily_unavailable,
             is_approved, is_active, created_at)
            VALUES
            (@id, @operator_id, @bus_name, @bus_number, @capacity, @layout_name, @layout_json, FALSE,
             FALSE, TRUE, @created_at);", connection);
        command.Parameters.AddWithValue("id", busId);
        command.Parameters.AddWithValue("operator_id", request.OperatorId);
        command.Parameters.AddWithValue("bus_name", request.BusName.Trim());
        command.Parameters.AddWithValue("bus_number", request.BusNumber.Trim());
        command.Parameters.AddWithValue("capacity", request.Capacity);
        command.Parameters.AddWithValue("layout_name", request.LayoutName.Trim());
        command.Parameters.AddWithValue("layout_json", request.LayoutJson ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.ExecuteNonQuery();

        return new BusResponse
        {
            BusId = busId,
            OperatorId = request.OperatorId,
            BusName = request.BusName.Trim(),
            Capacity = request.Capacity,
            IsTemporarilyUnavailable = false,
            IsApproved = false,
            IsActive = true,
            LayoutName = request.LayoutName.Trim()
        };
    }

    public IEnumerable<BusResponse> GetAllBuses()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_number, bus_name, capacity, is_temporarily_unavailable, is_approved, is_active, layout_name
            FROM buses
            ORDER BY created_at DESC;", connection);
        using var reader = command.ExecuteReader();

        var items = new List<BusResponse>();
        while (reader.Read())
        {
            items.Add(new BusResponse
            {
                BusId = reader.GetGuid(0),
                OperatorId = reader.GetGuid(1),
                BusNumber = reader.GetString(2),
                BusName = reader.GetString(3),
                Capacity = reader.GetInt32(4),
                IsTemporarilyUnavailable = reader.GetBoolean(5),
                IsApproved = reader.GetBoolean(6),
                IsActive = reader.GetBoolean(7),
                LayoutName = reader.GetString(8)
            });
        }

        return items;
    }

    public IEnumerable<BusResponse> GetOperatorBuses(Guid operatorId)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_number, bus_name, capacity, is_temporarily_unavailable, is_approved, is_active, layout_name
            FROM buses
            WHERE operator_id = @operator_id
            ORDER BY created_at DESC;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        using var reader = command.ExecuteReader();

        var items = new List<BusResponse>();
        while (reader.Read())
        {
            items.Add(new BusResponse
            {
                BusId = reader.GetGuid(0),
                OperatorId = reader.GetGuid(1),
                BusNumber = reader.GetString(2),
                BusName = reader.GetString(3),
                Capacity = reader.GetInt32(4),
                IsTemporarilyUnavailable = reader.GetBoolean(5),
                IsApproved = reader.GetBoolean(6),
                IsActive = reader.GetBoolean(7),
                LayoutName = reader.GetString(8)
            });
        }

        return items;
    }

    public BusResponse ApproveBus(Guid busId, ApprovalRequest request)
    {
        using var connection = OpenConnection();
        var bus = GetBus(connection, busId);

        using var command = new NpgsqlCommand(@"
            UPDATE buses
            SET is_approved = @is_approved
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("is_approved", request.Approve);
        command.Parameters.AddWithValue("id", busId);
        command.ExecuteNonQuery();

        return new BusResponse
        {
            BusId = bus.Id,
            OperatorId = bus.OperatorId,
            BusNumber = bus.BusNumber,
            BusName = bus.BusName,
            Capacity = bus.Capacity,
            IsTemporarilyUnavailable = bus.IsTemporarilyUnavailable,
            IsApproved = request.Approve,
            IsActive = bus.IsActive,
            LayoutName = bus.LayoutName
        };
    }

    public BusResponse SetBusTemporaryAvailability(Guid operatorId, Guid busId, bool unavailable)
    {
        using var connection = OpenConnection();
        var bus = GetBus(connection, busId);
        if (bus.OperatorId != operatorId)
        {
            throw new KeyNotFoundException("Bus not found for this operator.");
        }

        using var command = new NpgsqlCommand(@"
            UPDATE buses
            SET is_temporarily_unavailable = @unavailable
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("unavailable", unavailable);
        command.Parameters.AddWithValue("id", busId);
        command.ExecuteNonQuery();

        return (bus with { IsTemporarilyUnavailable = unavailable }).ToResponse();
    }

    public void RemoveBus(Guid operatorId, Guid busId)
    {
        using var connection = OpenConnection();
        var bus = GetBus(connection, busId);
        if (bus.OperatorId != operatorId)
        {
            throw new KeyNotFoundException("Bus not found for this operator.");
        }

        using var command = new NpgsqlCommand(@"
            UPDATE buses
            SET is_active = FALSE
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", busId);
        command.ExecuteNonQuery();
    }

    public BusResponse DisableBus(Guid busId, DisableBusRequest request)
    {
        using var connection = OpenConnection();
        var bus = GetBus(connection, busId);

        using var command = new NpgsqlCommand(@"
            UPDATE buses
            SET is_active = FALSE
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", busId);
        command.ExecuteNonQuery();

        CreateNotification(connection, null, "admin@system.com", "Bus disabled by Admin", $"Bus '{bus.BusName}' was disabled by admin. Reason: {request.Reason}");
        CreateNotification(connection, null, bus.OperatorId.ToString(), "Bus disabled", $"Your bus '{bus.BusName}' was disabled by admin. Reason: {request.Reason}");

        return (bus with { IsActive = false }).ToResponse();
    }

    public TripSummary AddTrip(TripCreateRequest request)
    {
        using var connection = OpenConnection();
        var operatorData = GetOperator(connection, request.OperatorId);
        if (operatorData.IsDisabled || ParseApprovalStatus(operatorData.ApprovalStatus) != OperatorApprovalStatus.Approved)
        {
            throw new InvalidOperationException("Operator is not active/approved.");
        }

        var bus = GetBus(connection, request.BusId);
        if (bus.OperatorId != request.OperatorId)
        {
            throw new InvalidOperationException("Bus does not belong to operator.");
        }

        if (!bus.IsApproved)
        {
            throw new InvalidOperationException("Bus must be admin-approved before adding to routes.");
        }

        if (bus.IsTemporarilyUnavailable || !bus.IsActive)
        {
            throw new InvalidOperationException("Bus is unavailable.");
        }

        var route = GetRoute(connection, request.RouteId);
        var tripId = Guid.NewGuid();
        using var command = new NpgsqlCommand(@"
            INSERT INTO trips
            (id, operator_id, bus_id, route_id, departure_time, arrival_time, base_price,
             platform_fee, is_variable_price, pickup_points, drop_points, is_active, created_at)
            VALUES
            (@id, @operator_id, @bus_id, @route_id, @departure_time, @arrival_time, @base_price,
             @platform_fee, @is_variable_price, @pickup_points, @drop_points, TRUE, @created_at);", connection);

        command.Parameters.AddWithValue("id", tripId);
        command.Parameters.AddWithValue("operator_id", request.OperatorId);
        command.Parameters.AddWithValue("bus_id", request.BusId);
        command.Parameters.AddWithValue("route_id", request.RouteId);
        command.Parameters.AddWithValue("departure_time", request.DepartureTime);
        command.Parameters.AddWithValue("arrival_time", request.ArrivalTime);
        command.Parameters.AddWithValue("base_price", request.BasePrice);
        command.Parameters.AddWithValue("platform_fee", request.PlatformFee);
        command.Parameters.AddWithValue("is_variable_price", request.IsVariablePrice);
        command.Parameters.AddWithValue("pickup_points", request.PickupPoints ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("drop_points", request.DropPoints ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.ExecuteNonQuery();

        return new TripSummary
        {
            TripId = tripId,
            BusId = request.BusId,
            BusName = bus.BusName,
            Source = route.Source,
            Destination = route.Destination,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            Capacity = bus.Capacity,
            SeatsAvailable = bus.Capacity,
            BasePrice = request.BasePrice,
            PlatformFee = request.PlatformFee,
            IsVariablePrice = request.IsVariablePrice
        };
    }

    public OperatorDashboardResponse GetOperatorDashboard(Guid operatorId)
    {
        using var connection = OpenConnection();
        _ = GetOperator(connection, operatorId);

        var totalBuses = ExecuteScalarInt(connection, "SELECT COUNT(*) FROM buses WHERE operator_id = @operator_id AND is_active = TRUE;", operatorId);
        var activeTrips = ExecuteScalarInt(connection, "SELECT COUNT(*) FROM trips WHERE operator_id = @operator_id AND is_active = TRUE AND departure_time > @now;", operatorId, DateTime.UtcNow);
        var totalBookings = ExecuteScalarInt(connection, @"
            SELECT COUNT(*)
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE t.operator_id = @operator_id AND b.is_cancelled = FALSE;", operatorId);

        decimal totalRevenue;
        using (var revenueCommand = new NpgsqlCommand(@"
            SELECT COALESCE(SUM(
                (CASE WHEN t.is_variable_price = TRUE THEN t.base_price * 1.10 ELSE t.base_price END)
                * COALESCE(array_length(b.seat_numbers, 1), 0)
            ), 0)
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE t.operator_id = @operator_id AND b.is_cancelled = FALSE;", connection))
        {
            revenueCommand.Parameters.AddWithValue("operator_id", operatorId);
            totalRevenue = Convert.ToDecimal(revenueCommand.ExecuteScalar());
        }

        return new OperatorDashboardResponse
        {
            OperatorId = operatorId,
            TotalBuses = totalBuses,
            ActiveTrips = activeTrips,
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue
        };
    }

    public AdminRevenueResponse GetAdminRevenue()
    {
        using var connection = OpenConnection();

        decimal totalPlatformFeeRevenue = 0;
        decimal platformFeeRevenuePastMonth = 0;
        decimal platformFeeRevenueThisMonth = 0;
        int totalBookings = 0;

        using (var revenueCommand = new NpgsqlCommand(@"
            SELECT
                COALESCE(SUM(t.platform_fee * COALESCE(array_length(b.seat_numbers, 1), 0)), 0) AS total_platform_fee_revenue,
                COUNT(b.id) AS total_bookings
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE b.is_cancelled = FALSE;", connection))
        {
            using var reader = revenueCommand.ExecuteReader();
            if (reader.Read())
            {
                totalPlatformFeeRevenue = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                totalBookings = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var pastMonthStart = monthStart.AddMonths(-1);

        using (var monthCommand = new NpgsqlCommand(@"
            SELECT
                COALESCE(SUM(CASE WHEN b.created_at >= @month_start THEN t.platform_fee * COALESCE(array_length(b.seat_numbers, 1), 0) ELSE 0 END), 0) AS this_month,
                COALESCE(SUM(CASE WHEN b.created_at >= @past_month_start AND b.created_at < @month_start THEN t.platform_fee * COALESCE(array_length(b.seat_numbers, 1), 0) ELSE 0 END), 0) AS past_month
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE b.is_cancelled = FALSE;", connection))
        {
            monthCommand.Parameters.AddWithValue("month_start", monthStart);
            monthCommand.Parameters.AddWithValue("past_month_start", pastMonthStart);
            using var reader = monthCommand.ExecuteReader();
            if (reader.Read())
            {
                platformFeeRevenueThisMonth = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                platformFeeRevenuePastMonth = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            }
        }

        return new AdminRevenueResponse
        {
            TotalPlatformFeeRevenue = totalPlatformFeeRevenue,
            PlatformFeeRevenueThisMonth = platformFeeRevenueThisMonth,
            PlatformFeeRevenuePastMonth = platformFeeRevenuePastMonth,
            TotalBookings = totalBookings
        };
    }

    public IEnumerable<NotificationResponse> GetNotifications(string recipientEmail)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, recipient_email, subject, message, created_at
            FROM notifications
            WHERE recipient_email = @recipient_email
            ORDER BY created_at DESC;", connection);
        command.Parameters.AddWithValue("recipient_email", NormalizeEmail(recipientEmail));

        using var reader = command.ExecuteReader();
        var items = new List<NotificationResponse>();
        while (reader.Read())
        {
            items.Add(new NotificationResponse
            {
                NotificationId = reader.GetGuid(0),
                RecipientEmail = reader.GetString(1),
                Subject = reader.GetString(2),
                Message = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return items;
    }

    private List<TripSummary> FindTrips(string source, string destination, DateOnly date)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT t.id, t.bus_id, b.bus_name, r.source, r.destination, t.departure_time, t.arrival_time,
                   b.capacity, t.base_price, t.platform_fee, t.is_variable_price
            FROM trips t
            JOIN routes r ON r.id = t.route_id
            JOIN buses b ON b.id = t.bus_id
            JOIN operators o ON o.id = t.operator_id
            WHERE t.is_active = TRUE
              AND b.is_active = TRUE
              AND b.is_temporarily_unavailable = FALSE
              AND o.approval_status = 'Approved'
              AND o.is_disabled = FALSE
              AND LOWER(r.source) = LOWER(@source)
              AND LOWER(r.destination) = LOWER(@destination)
              AND (
                  (t.trip_type = 'OneTime' AND DATE(t.departure_time) = @travel_date)
                  OR
                  (t.trip_type = 'Daily' AND DATE(t.departure_time) <= @travel_date AND t.days_of_week LIKE '%' || @day_of_week || '%')
              )
            ORDER BY t.departure_time;", connection);

        command.Parameters.AddWithValue("source", source.Trim());
        command.Parameters.AddWithValue("destination", destination.Trim());
        command.Parameters.AddWithValue("travel_date", date.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("day_of_week", date.ToString("ddd"));

        using var reader = command.ExecuteReader();
        var rawTrips = new List<(Guid TripId, Guid BusId, string BusName, string Source, string Destination, DateTime Departure, DateTime Arrival, int Capacity, decimal BasePrice, decimal PlatformFee, bool IsVariable)>();
        while (reader.Read())
        {
            var originalDeparture = reader.GetDateTime(5);
            var originalArrival = reader.GetDateTime(6);
            var daysOffset = (date.ToDateTime(TimeOnly.MinValue).Date - originalDeparture.Date).Days;
            var actualDeparture = originalDeparture.AddDays(daysOffset);
            var actualArrival = originalArrival.AddDays(daysOffset);

            rawTrips.Add((
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                actualDeparture,
                actualArrival,
                reader.GetInt32(7),
                reader.GetDecimal(8),
                reader.GetDecimal(9),
                reader.GetBoolean(10)
            ));
        }

        reader.Close();

        var trips = new List<TripSummary>();
        foreach (var item in rawTrips)
        {
            var seatsAvailable = item.Capacity - GetReservedSeatNumbers(connection, item.TripId, date).Count;

            trips.Add(new TripSummary
            {
                TripId = item.TripId,
                BusId = item.BusId,
                BusName = item.BusName,
                Source = item.Source,
                Destination = item.Destination,
                DepartureTime = item.Departure,
                ArrivalTime = item.Arrival,
                Capacity = item.Capacity,
                SeatsAvailable = Math.Max(0, seatsAvailable),
                BasePrice = item.BasePrice,
                PlatformFee = item.PlatformFee,
                IsVariablePrice = item.IsVariable
            });
        }

        return trips;
    }

    private HashSet<int> GetReservedSeatNumbers(NpgsqlConnection connection, Guid tripId, DateOnly travelDate)
    {
        var reserved = new HashSet<int>();

        using (var bookedCommand = new NpgsqlCommand(@"
            SELECT seat_numbers
            FROM bookings
            WHERE trip_id = @trip_id AND travel_date = @travel_date AND is_cancelled = FALSE;", connection))
        {
            bookedCommand.Parameters.AddWithValue("trip_id", tripId);
            bookedCommand.Parameters.AddWithValue("travel_date", travelDate.ToDateTime(TimeOnly.MinValue));
            using var reader = bookedCommand.ExecuteReader();
            while (reader.Read())
            {
                foreach (var seat in reader.GetFieldValue<int[]>(0))
                {
                    reserved.Add(seat);
                }
            }
        }

        using (var lockCommand = new NpgsqlCommand(@"
            SELECT seat_numbers
            FROM seat_locks
            WHERE trip_id = @trip_id AND travel_date = @travel_date
              AND expires_at > @now;", connection))
        {
            lockCommand.Parameters.AddWithValue("trip_id", tripId);
            lockCommand.Parameters.AddWithValue("travel_date", travelDate.ToDateTime(TimeOnly.MinValue));
            lockCommand.Parameters.AddWithValue("now", DateTime.UtcNow);
            using var reader = lockCommand.ExecuteReader();
            while (reader.Read())
            {
                foreach (var seat in reader.GetFieldValue<int[]>(0))
                {
                    reserved.Add(seat);
                }
            }
        }

        return reserved;
    }

    private void DeleteExpiredLocks()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand("DELETE FROM seat_locks WHERE expires_at <= @now;", connection);
        command.Parameters.AddWithValue("now", DateTime.UtcNow);
        command.ExecuteNonQuery();
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY,
                email TEXT NOT NULL UNIQUE,
                full_name TEXT NOT NULL,
                sso_provider TEXT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS operators (
                id UUID PRIMARY KEY,
                company_name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                approval_status TEXT NOT NULL,
                is_disabled BOOLEAN NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS routes (
                id UUID PRIMARY KEY,
                source TEXT NOT NULL,
                destination TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                UNIQUE (source, destination)
            );

            CREATE TABLE IF NOT EXISTS buses (
                id UUID PRIMARY KEY,
                operator_id UUID NOT NULL REFERENCES operators(id),
                bus_name TEXT NOT NULL,
                bus_number TEXT NOT NULL UNIQUE,
                capacity INTEGER NOT NULL,
                layout_name TEXT NOT NULL,
                layout_json TEXT NULL,
                is_temporarily_unavailable BOOLEAN NOT NULL,
                is_approved BOOLEAN NOT NULL,
                is_active BOOLEAN NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS trips (
                id UUID PRIMARY KEY,
                operator_id UUID NOT NULL REFERENCES operators(id),
                bus_id UUID NOT NULL REFERENCES buses(id),
                route_id UUID NOT NULL REFERENCES routes(id),
                departure_time TIMESTAMPTZ NOT NULL,
                arrival_time TIMESTAMPTZ NOT NULL,
                base_price NUMERIC(10,2) NOT NULL,
                platform_fee NUMERIC(10,2) NOT NULL,
                is_variable_price BOOLEAN NOT NULL,
                pickup_points TEXT NULL,
                drop_points TEXT NULL,
                trip_type TEXT NOT NULL DEFAULT 'OneTime',
                days_of_week TEXT NULL,
                is_active BOOLEAN NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS seat_locks (
                id UUID PRIMARY KEY,
                trip_id UUID NOT NULL REFERENCES trips(id),
                user_email TEXT NOT NULL,
                seat_numbers INTEGER[] NOT NULL,
                expires_at TIMESTAMPTZ NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS bookings (
                id UUID PRIMARY KEY,
                pnr TEXT NOT NULL UNIQUE,
                trip_id UUID NOT NULL REFERENCES trips(id),
                user_email TEXT NOT NULL,
                seat_numbers INTEGER[] NOT NULL,
                total_amount NUMERIC(10,2) NOT NULL,
                payment_mode TEXT NOT NULL,
                payment_status TEXT NOT NULL,
                ticket_download_url TEXT NOT NULL,
                mail_status TEXT NOT NULL,
                is_cancelled BOOLEAN NOT NULL,
                refund_amount NUMERIC(10,2) NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS booking_passengers (
                id UUID PRIMARY KEY,
                booking_id UUID NOT NULL REFERENCES bookings(id),
                seat_number INTEGER NOT NULL,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                gender TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS notifications (
                id UUID PRIMARY KEY,
                recipient_email TEXT NOT NULL,
                subject TEXT NOT NULL,
                message TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS operator_preferred_routes (
                id UUID PRIMARY KEY,
                operator_id UUID NOT NULL REFERENCES operators(id),
                route_id UUID NOT NULL REFERENCES routes(id),
                created_at TIMESTAMPTZ NOT NULL,
                UNIQUE (operator_id, route_id)
            );

            CREATE TABLE IF NOT EXISTS pickup_drop_points (
                id UUID PRIMARY KEY,
                operator_id UUID NOT NULL REFERENCES operators(id),
                route_id UUID NOT NULL REFERENCES routes(id),
                is_pickup BOOLEAN NOT NULL,
                location TEXT NOT NULL,
                address TEXT NULL,
                is_default BOOLEAN NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                UNIQUE (operator_id, route_id, is_pickup, location)
            );

            CREATE TABLE IF NOT EXISTS platform_fees (
                id UUID PRIMARY KEY,
                amount NUMERIC(10,2) NOT NULL,
                description TEXT NULL,
                is_active BOOLEAN NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            -- Ensure unique constraint for pickup_drop_points exists for upsert logic
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_operator_route_pickup_v2') THEN
                    ALTER TABLE pickup_drop_points DROP CONSTRAINT IF EXISTS uq_operator_route_pickup;
                    ALTER TABLE pickup_drop_points ADD CONSTRAINT uq_operator_route_pickup_v2 UNIQUE (operator_id, route_id, is_pickup, location);
                END IF;
            END $$;", connection);

        command.ExecuteNonQuery();

        // Add trip_type and days_of_week columns if they don't exist (idempotent migration)
        using var alterCommand = new NpgsqlCommand(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='trips' AND column_name='trip_type') THEN
                    ALTER TABLE trips ADD COLUMN trip_type TEXT NOT NULL DEFAULT 'OneTime';
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='trips' AND column_name='days_of_week') THEN
                    ALTER TABLE trips ADD COLUMN days_of_week TEXT NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='trips' AND column_name='arrival_day_offset') THEN
                    ALTER TABLE trips ADD COLUMN arrival_day_offset INT NOT NULL DEFAULT 0;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='trips' AND column_name='start_date') THEN
                    ALTER TABLE trips ADD COLUMN start_date DATE NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='trips' AND column_name='end_date') THEN
                    ALTER TABLE trips ADD COLUMN end_date DATE NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='trips' AND column_name='departure_time_only') THEN
                    ALTER TABLE trips ADD COLUMN departure_time_only TIME NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='trips' AND column_name='arrival_time_only') THEN
                    ALTER TABLE trips ADD COLUMN arrival_time_only TIME NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='bookings' AND column_name='travel_date') THEN
                    ALTER TABLE bookings ADD COLUMN travel_date DATE NOT NULL DEFAULT CURRENT_DATE;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='seat_locks' AND column_name='travel_date') THEN
                    ALTER TABLE seat_locks ADD COLUMN travel_date DATE NOT NULL DEFAULT CURRENT_DATE;
                END IF;
            END $$;", connection);
        alterCommand.ExecuteNonQuery();

        // Ensure active trips always use the currently active platform fee.
        using var syncFeeCommand = new NpgsqlCommand(@"
            UPDATE trips t
            SET platform_fee = pf.amount
            FROM (
                SELECT amount
                FROM platform_fees
                WHERE is_active = TRUE
                ORDER BY created_at DESC
                LIMIT 1
            ) pf
            WHERE t.is_active = TRUE;", connection);
        syncFeeCommand.ExecuteNonQuery();
    }

    private void SeedDemoData()
    {
        using var connection = OpenConnection();

        using var operatorCheck = new NpgsqlCommand("SELECT COUNT(*) FROM operators;", connection);
        var count = Convert.ToInt32(operatorCheck.ExecuteScalar());
        if (count > 0)
        {
            return;
        }

        var operatorId = Guid.NewGuid();
        var busId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        using var transaction = connection.BeginTransaction();
        using (var insertOperator = new NpgsqlCommand(@"
            INSERT INTO operators (id, company_name, email, password_hash, approval_status, is_disabled, created_at)
            VALUES (@id, @company_name, @email, @password_hash, 'Approved', FALSE, @created_at);", connection, transaction))
        {
            insertOperator.Parameters.AddWithValue("id", operatorId);
            insertOperator.Parameters.AddWithValue("company_name", "Demo Travels");
            insertOperator.Parameters.AddWithValue("email", "operator@demo.com");
            insertOperator.Parameters.AddWithValue("password_hash", passwordHasher.HashPassword("operator@demo.com", "Password@123"));
            insertOperator.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            insertOperator.ExecuteNonQuery();
        }

        using (var insertRoute = new NpgsqlCommand(@"
            INSERT INTO routes (id, source, destination, created_at)
            VALUES (@id, @source, @destination, @created_at);", connection, transaction))
        {
            insertRoute.Parameters.AddWithValue("id", routeId);
            insertRoute.Parameters.AddWithValue("source", "Chennai");
            insertRoute.Parameters.AddWithValue("destination", "Bangalore");
            insertRoute.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            insertRoute.ExecuteNonQuery();
        }

        using (var insertBus = new NpgsqlCommand(@"
            INSERT INTO buses
            (id, operator_id, bus_name, bus_number, capacity, layout_name, layout_json, is_temporarily_unavailable, is_approved, is_active, created_at)
            VALUES
            (@id, @operator_id, @bus_name, @bus_number, @capacity, @layout_name, @layout_json, FALSE, TRUE, TRUE, @created_at);", connection, transaction))
        {
            insertBus.Parameters.AddWithValue("id", busId);
            insertBus.Parameters.AddWithValue("operator_id", operatorId);
            insertBus.Parameters.AddWithValue("bus_name", "Demo AC Sleeper");
            insertBus.Parameters.AddWithValue("bus_number", "DEMO-1234");
            insertBus.Parameters.AddWithValue("capacity", 40);
            insertBus.Parameters.AddWithValue("layout_name", "2+2");
            insertBus.Parameters.AddWithValue("layout_json", "{\"rows\":10,\"columns\":4}");
            insertBus.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            insertBus.ExecuteNonQuery();
        }

        using (var insertTrip = new NpgsqlCommand(@"
            INSERT INTO trips
            (id, operator_id, bus_id, route_id, departure_time, arrival_time, base_price, platform_fee, is_variable_price, pickup_points, drop_points, is_active, created_at)
            VALUES
            (@id, @operator_id, @bus_id, @route_id, @departure_time, @arrival_time, @base_price, @platform_fee, FALSE, @pickup_points, @drop_points, TRUE, @created_at);", connection, transaction))
        {
            insertTrip.Parameters.AddWithValue("id", tripId);
            insertTrip.Parameters.AddWithValue("operator_id", operatorId);
            insertTrip.Parameters.AddWithValue("bus_id", busId);
            insertTrip.Parameters.AddWithValue("route_id", routeId);
            insertTrip.Parameters.AddWithValue("departure_time", DateTime.UtcNow.AddDays(1).Date.AddHours(21));
            insertTrip.Parameters.AddWithValue("arrival_time", DateTime.UtcNow.AddDays(2).Date.AddHours(6));
            insertTrip.Parameters.AddWithValue("base_price", 750m);
            insertTrip.Parameters.AddWithValue("platform_fee", 25m);
            insertTrip.Parameters.AddWithValue("pickup_points", "Koyambedu,Guindy");
            insertTrip.Parameters.AddWithValue("drop_points", "Madiwala,Majestic");
            insertTrip.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            insertTrip.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private void CreateNotification(NpgsqlConnection connection, NpgsqlTransaction? transaction, string recipient, string subject, string message)
    {
        using var command = new NpgsqlCommand(@"
            INSERT INTO notifications (id, recipient_email, subject, message, created_at)
            VALUES (@id, @recipient_email, @subject, @message, @created_at);", connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("recipient_email", recipient);
        command.Parameters.AddWithValue("subject", subject);
        command.Parameters.AddWithValue("message", message);
        command.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        command.ExecuteNonQuery();
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        return email.Trim().ToLowerInvariant();
    }

    private OperatorRecord GetOperator(NpgsqlConnection connection, Guid operatorId)
    {
        using var command = new NpgsqlCommand(@"
            SELECT id, company_name, email, approval_status, is_disabled
            FROM operators
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", operatorId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Operator not found.");
        }

        return new OperatorRecord(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetBoolean(4));
    }

    private BusRecord GetBus(NpgsqlConnection connection, Guid busId)
    {
        using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_number, bus_name, capacity, is_temporarily_unavailable, is_approved, is_active, layout_name
            FROM buses
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", busId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Bus not found.");
        }

        return new BusRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetBoolean(5),
            reader.GetBoolean(6),
            reader.GetBoolean(7),
            reader.GetString(8));
    }

    private RouteResponse GetRoute(NpgsqlConnection connection, Guid routeId)
    {
        using var command = new NpgsqlCommand(@"
            SELECT id, source, destination
            FROM routes
            WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", routeId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Route not found.");
        }

        return new RouteResponse
        {
            RouteId = reader.GetGuid(0),
            Source = reader.GetString(1),
            Destination = reader.GetString(2)
        };
    }

    private static OperatorApprovalStatus ParseApprovalStatus(string status)
    {
        return Enum.TryParse<OperatorApprovalStatus>(status, out var parsed)
            ? parsed
            : OperatorApprovalStatus.Pending;
    }

    private static int ExecuteScalarInt(NpgsqlConnection connection, string sql, Guid operatorId, DateTime? now = null)
    {
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        if (now.HasValue)
        {
            command.Parameters.AddWithValue("now", now.Value);
        }

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public LoginResponse Login(CustomerLoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, email, password_hash, is_profile_completed, first_name, last_name
            FROM registration_sessions
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("email", email);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("User not found. Please register first.");
        }

        var userId = reader.GetGuid(reader.GetOrdinal("id"));
        var passwordHash = reader.IsDBNull(reader.GetOrdinal("password_hash"))
            ? null
            : reader.GetString(reader.GetOrdinal("password_hash"));
        var profileCompleted = reader.GetBoolean(reader.GetOrdinal("is_profile_completed"));
        var firstName = reader.IsDBNull(reader.GetOrdinal("first_name"))
            ? null
            : reader.GetString(reader.GetOrdinal("first_name"));
        var lastName = reader.IsDBNull(reader.GetOrdinal("last_name"))
            ? null
            : reader.GetString(reader.GetOrdinal("last_name"));
        reader.Close();

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new InvalidOperationException("Password is not set. Complete registration first.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(email, passwordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Invalid password.");
        }

        var fullName = BuildFullName(firstName, lastName, email);

        var jwtToken = GenerateJwtToken(userId, email);

        return new LoginResponse
        {
            UserId = userId,
            Email = email,
            FullName = fullName,
            JwtToken = jwtToken,
            Message = "Login successful"
        };
    }

    private static string BuildFullName(string? firstName, string? lastName, string fallbackEmail)
    {
        var fullName = string.Join(" ", new[] { firstName, lastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));

        return string.IsNullOrWhiteSpace(fullName) ? fallbackEmail : fullName;
    }

    public TripSearchResponse SearchTripsFuzzy(string source, string destination, DateOnly date, DateOnly? returnDate)
    {
        DeleteExpiredLocks();

        var outbound = FindTripsFuzzy(source, destination, date);
        var response = new TripSearchResponse { OutboundTrips = outbound };

        if (returnDate.HasValue)
        {
            response.ReturnTrips = FindTripsFuzzy(destination, source, returnDate.Value);
        }

        return response;
    }

    public SeatLayoutResponse GetSeatLayout(Guid tripId, DateOnly travelDate)
    {
        DeleteExpiredLocks();

        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT t.id, t.operator_id, t.route_id, b.bus_name, b.capacity, b.layout_name, r.source, r.destination
            FROM trips t
            JOIN buses b ON b.id = t.bus_id
            JOIN routes r ON r.id = t.route_id
            WHERE t.id = @trip_id AND t.is_active = TRUE;", connection);
        command.Parameters.AddWithValue("trip_id", tripId);

        Guid operatorId;
        Guid routeId;
        string busName;
        int capacity;
        string layoutName;

        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read())
            {
                throw new KeyNotFoundException("Trip not found.");
            }
            operatorId = reader.GetGuid(reader.GetOrdinal("operator_id"));
            routeId = reader.GetGuid(reader.GetOrdinal("route_id"));
            busName = reader.GetString(reader.GetOrdinal("bus_name"));
            capacity = reader.GetInt32(reader.GetOrdinal("capacity"));
            layoutName = reader.GetString(reader.GetOrdinal("layout_name"));
        }

        var (bookedSeats, lockedSeats) = GetReservedSeatsWithGender(connection, tripId, travelDate);
        var availableSeats = Enumerable.Range(1, capacity)
            .Where(s => !bookedSeats.ContainsKey(s) && !lockedSeats.Contains(s))
            .ToList();

        var seatsDict = new Dictionary<int, SeatWithGenderInfo>();
        for (int i = 1; i <= capacity; i++)
        {
            if (bookedSeats.TryGetValue(i, out var seatInfo))
            {
                seatsDict[i] = new SeatWithGenderInfo
                {
                    SeatNumber = i,
                    IsAvailable = false,
                    BookedByGender = seatInfo.Gender,
                    BookedByName = seatInfo.Name
                };
            }
            else if (lockedSeats.Contains(i))
            {
                seatsDict[i] = new SeatWithGenderInfo
                {
                    SeatNumber = i,
                    IsAvailable = false,
                    BookedByGender = "locked"
                };
            }
            else
            {
                seatsDict[i] = new SeatWithGenderInfo
                {
                    SeatNumber = i,
                    IsAvailable = true
                };
            }
        }

        var ladiesSeats = Enumerable.Range(1, capacity)
            .Where(s => bookedSeats.ContainsKey(s) && 
                        bookedSeats[s].Gender == "Female" && 
                        !lockedSeats.Contains(s))
            .ToList();

        return new SeatLayoutResponse
        {
            TripId = tripId,
            TravelDate = travelDate,
            BusName = busName,
            LayoutName = layoutName,
            Capacity = capacity,
            SeatsAvailableLeft = availableSeats.Count,
            Seats = seatsDict,
            LadiesSeatsAvailable = ladiesSeats,
            PickupPoints = GetPickupDropPoints(operatorId, routeId, true).ToList(),
            DropPoints = GetPickupDropPoints(operatorId, routeId, false).ToList()
        };
    }

    public EnhancedBookingResponse GetEnhancedBooking(Guid bookingId, string userEmail)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT b.id, b.pnr, b.trip_id, b.seat_numbers, b.total_amount, b.refund_amount,
                   b.is_cancelled, b.ticket_download_url, b.payment_status, b.created_at,
                   t.departure_time, t.arrival_time, r.source, r.destination, bus.bus_name
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            JOIN routes r ON r.id = t.route_id
            JOIN buses bus ON bus.id = t.bus_id
            WHERE b.id = @booking_id AND b.user_email = @user_email;", connection);
        command.Parameters.AddWithValue("booking_id", bookingId);
        command.Parameters.AddWithValue("user_email", NormalizeEmail(userEmail));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        var seatNumbers = reader.GetFieldValue<int[]>(reader.GetOrdinal("seat_numbers")).ToList();
        var departureTime = reader.GetDateTime(reader.GetOrdinal("departure_time"));
        var arrivalTime = reader.GetDateTime(reader.GetOrdinal("arrival_time"));
        var createdAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
        var isCancelled = reader.GetBoolean(reader.GetOrdinal("is_cancelled"));
        var status = isCancelled ? "Cancelled" : 
                     (departureTime < DateTime.UtcNow ? "Completed" : "Upcoming");

        var booking = new EnhancedBookingResponse
        {
            BookingId = reader.GetGuid(reader.GetOrdinal("id")),
            Pnr = reader.GetString(reader.GetOrdinal("pnr")),
            TripId = reader.GetGuid(reader.GetOrdinal("trip_id")),
            BusName = reader.GetString(reader.GetOrdinal("bus_name")),
            Source = reader.GetString(reader.GetOrdinal("source")),
            Destination = reader.GetString(reader.GetOrdinal("destination")),
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            SeatNumbers = seatNumbers,
            TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
            RefundAmount = reader.GetDecimal(reader.GetOrdinal("refund_amount")),
            IsCancelled = isCancelled,
            PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status")),
            TicketUrl = reader.GetString(reader.GetOrdinal("ticket_download_url")),
            BookedAt = createdAt,
            Status = status
        };
        reader.Close();

        using var passengerCommand = new NpgsqlCommand(@"
            SELECT name, age, gender, seat_number
            FROM booking_passengers
            WHERE booking_id = @booking_id;", connection);
        passengerCommand.Parameters.AddWithValue("booking_id", bookingId);

        using var passengerReader = passengerCommand.ExecuteReader();
        while (passengerReader.Read())
        {
            booking.Passengers.Add(new BookingPassenger
            {
                Name = passengerReader.GetString(passengerReader.GetOrdinal("name")),
                Age = passengerReader.GetInt32(passengerReader.GetOrdinal("age")),
                Gender = passengerReader.GetString(passengerReader.GetOrdinal("gender")),
                SeatNumber = passengerReader.GetInt32(passengerReader.GetOrdinal("seat_number"))
            });
        }

        return booking;
    }

    public IEnumerable<EnhancedBookingResponse> GetBookingsHistory(BookingsHistoryFilter filter)
    {
        using var connection = OpenConnection();
        string sql = @"
            SELECT b.id, b.pnr, b.trip_id, b.seat_numbers, b.total_amount, b.refund_amount,
                   b.is_cancelled, b.ticket_download_url, b.payment_status, b.created_at,
                   t.departure_time, t.arrival_time, r.source, r.destination, bus.bus_name
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            JOIN routes r ON r.id = t.route_id
            JOIN buses bus ON bus.id = t.bus_id
            WHERE b.user_email = @user_email ";

        var now = DateTime.UtcNow;
        switch (filter.Type)
        {
            case BookingsHistoryFilter.HistoryType.Past:
                sql += "AND t.departure_time < @now AND b.is_cancelled = FALSE ";
                break;
            case BookingsHistoryFilter.HistoryType.Present:
                sql += "AND t.departure_time >= @now AND t.departure_time <= @now + INTERVAL '7 days' AND b.is_cancelled = FALSE ";
                break;
            case BookingsHistoryFilter.HistoryType.Future:
                sql += "AND t.departure_time > @now + INTERVAL '7 days' AND b.is_cancelled = FALSE ";
                break;
            case BookingsHistoryFilter.HistoryType.Cancelled:
                sql += "AND b.is_cancelled = TRUE ";
                break;
        }

        sql += "ORDER BY b.created_at DESC;";

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_email", NormalizeEmail(filter.UserEmail));
        command.Parameters.AddWithValue("now", now);

        using var reader = command.ExecuteReader();
        var bookings = new List<EnhancedBookingResponse>();
        var bookingDict = new Dictionary<Guid, EnhancedBookingResponse>();

        while (reader.Read())
        {
            var bookingId = reader.GetGuid(reader.GetOrdinal("id"));
            if (!bookingDict.TryGetValue(bookingId, out var booking))
            {
                var departureTime = reader.GetDateTime(reader.GetOrdinal("departure_time"));
                var isCancelled = reader.GetBoolean(reader.GetOrdinal("is_cancelled"));
                var status = isCancelled ? "Cancelled" : 
                             (departureTime < now ? "Completed" : "Upcoming");

                booking = new EnhancedBookingResponse
                {
                    BookingId = bookingId,
                    Pnr = reader.GetString(reader.GetOrdinal("pnr")),
                    TripId = reader.GetGuid(reader.GetOrdinal("trip_id")),
                    BusName = reader.GetString(reader.GetOrdinal("bus_name")),
                    Source = reader.GetString(reader.GetOrdinal("source")),
                    Destination = reader.GetString(reader.GetOrdinal("destination")),
                    DepartureTime = departureTime,
                    ArrivalTime = reader.GetDateTime(reader.GetOrdinal("arrival_time")),
                    SeatNumbers = reader.GetFieldValue<int[]>(reader.GetOrdinal("seat_numbers")).ToList(),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                    RefundAmount = reader.GetDecimal(reader.GetOrdinal("refund_amount")),
                    IsCancelled = isCancelled,
                    PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status")),
                    TicketUrl = $"/api/bookings/{bookingId}/ticket/download",
                    BookedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    Status = status
                };
                bookingDict[bookingId] = booking;
                bookings.Add(booking);
            }
        }

        return bookings;
    }

    public TicketResponse GetTicket(Guid bookingId, string userEmail)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT b.id, b.pnr, b.payment_status, b.ticket_download_url, b.total_amount,
                   t.departure_time, t.arrival_time, r.source, r.destination, bus.bus_name
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            JOIN routes r ON r.id = t.route_id
            JOIN buses bus ON bus.id = t.bus_id
            WHERE b.id = @booking_id AND b.user_email = @user_email;", connection);
        command.Parameters.AddWithValue("booking_id", bookingId);
        command.Parameters.AddWithValue("user_email", NormalizeEmail(userEmail));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        var ticket = new TicketResponse
        {
            BookingId = reader.GetGuid(reader.GetOrdinal("id")),
            Pnr = reader.GetString(reader.GetOrdinal("pnr")),
            BusName = reader.GetString(reader.GetOrdinal("bus_name")),
            Source = reader.GetString(reader.GetOrdinal("source")),
            Destination = reader.GetString(reader.GetOrdinal("destination")),
            DepartureTime = reader.GetDateTime(reader.GetOrdinal("departure_time")),
            ArrivalTime = reader.GetDateTime(reader.GetOrdinal("arrival_time")),
            TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
            PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status")),
            TicketUrl = $"/api/bookings/{bookingId}/ticket/download"
        };
        reader.Close();

        using var passengerCommand = new NpgsqlCommand(@"
            SELECT name, age, gender, seat_number
            FROM booking_passengers
            WHERE booking_id = @booking_id
            ORDER BY seat_number;", connection);
        passengerCommand.Parameters.AddWithValue("booking_id", bookingId);

        using var passengerReader = passengerCommand.ExecuteReader();
        while (passengerReader.Read())
        {
            ticket.Passengers.Add(new BookingPassenger
            {
                Name = passengerReader.GetString(passengerReader.GetOrdinal("name")),
                Age = passengerReader.GetInt32(passengerReader.GetOrdinal("age")),
                Gender = passengerReader.GetString(passengerReader.GetOrdinal("gender")),
                SeatNumber = passengerReader.GetInt32(passengerReader.GetOrdinal("seat_number"))
            });
        }

        return ticket;
    }

    public (byte[] Content, string FileName) GenerateTicketFile(Guid bookingId, string userEmail)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ticket = GetTicket(bookingId, userEmail);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Inch);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("BUS TICKET").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{ticket.BusName}").FontSize(14).Italic();
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text($"PNR: {ticket.Pnr}").FontSize(16).SemiBold();
                        col.Item().Text($"Date: {DateTime.Now:d}");
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(20);

                    // Trip Info
                    col.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FROM").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(ticket.Source).FontSize(14).SemiBold();
                            c.Item().Text(ticket.DepartureTime.ToString("f")).FontSize(10);
                        });

                        row.ConstantItem(50).AlignCenter().Text("→").FontSize(20);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("TO").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(ticket.Destination).FontSize(14).SemiBold();
                            c.Item().Text(ticket.ArrivalTime.ToString("f")).FontSize(10);
                        });
                    });

                    // Passenger Info
                    col.Item().Column(c =>
                    {
                        c.Item().PaddingBottom(5).Text("PASSENGERS").FontSize(12).SemiBold();
                        c.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Name");
                                header.Cell().Element(CellStyle).Text("Age");
                                header.Cell().Element(CellStyle).Text("Gender");
                                header.Cell().Element(CellStyle).Text("Seat");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var p in ticket.Passengers)
                            {
                                table.Cell().Element(CellStyle).Text(p.Name);
                                table.Cell().Element(CellStyle).Text(p.Age.ToString());
                                table.Cell().Element(CellStyle).Text(p.Gender);
                                table.Cell().Element(CellStyle).Text(p.SeatNumber.ToString());

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            }
                        });
                    });

                    // Summary
                    col.Item().AlignRight().Column(c =>
                    {
                        c.Spacing(5);
                        c.Item().Text($"Total Amount: {ticket.TotalAmount:C}").FontSize(14).SemiBold();
                        c.Item().Text($"Payment Status: {ticket.PaymentStatus}").FontSize(10).Italic();
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return (document.GeneratePdf(), $"Ticket_{ticket.Pnr}.pdf");
    }

    public PaymentInitiateResponse InitiatePayment(PaymentInitiateRequest request)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT total_amount FROM bookings WHERE id = @booking_id;", connection);
        command.Parameters.AddWithValue("booking_id", request.BookingId);

        var amount = (decimal?)command.ExecuteScalar();
        if (!amount.HasValue)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        var paymentId = Guid.NewGuid();
        string? razorpayOrderId = null;

        if (request.PaymentMode == PaymentMode.Razorpay)
        {
            razorpayOrderId = $"order_{Guid.NewGuid().ToString().Substring(0, 12)}";
        }

        return new PaymentInitiateResponse
        {
            PaymentId = paymentId,
            BookingId = request.BookingId,
            Amount = amount.Value,
            PaymentMode = request.PaymentMode,
            RazorpayOrderId = razorpayOrderId,
            Message = "Payment initiated. Proceed to payment gateway."
        };
    }

    public PaymentVerifyResponse VerifyPayment(PaymentVerifyRequest request)
    {
        using var connection = OpenConnection();
        using var updateCommand = new NpgsqlCommand(@"
            UPDATE bookings
            SET payment_status = 'PAID'
            WHERE id = @payment_id;", connection);
        updateCommand.Parameters.AddWithValue("payment_id", request.PaymentId);
        updateCommand.ExecuteNonQuery();

        return new PaymentVerifyResponse
        {
            PaymentId = request.PaymentId,
            BookingId = Guid.NewGuid(),
            Verified = true,
            Message = "Payment verified successfully."
        };
    }

    private List<TripSummary> FindTripsFuzzy(string source, string destination, DateOnly date)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT t.id, t.bus_id, b.bus_name, r.source, r.destination,
                   t.departure_time, t.arrival_time, b.capacity, t.base_price,
                   t.platform_fee, t.is_variable_price, t.trip_type, t.days_of_week,
                   t.start_date, t.end_date, t.arrival_day_offset, 
                   t.departure_time_only, t.arrival_time_only,
                   t.operator_id, t.route_id
            FROM trips t
            JOIN buses b ON b.id = t.bus_id
            JOIN routes r ON r.id = t.route_id
            JOIN operators o ON o.id = t.operator_id
            WHERE t.is_active = TRUE
              AND b.is_approved = TRUE AND b.is_active = TRUE
              AND o.approval_status = 'Approved' AND o.is_disabled = FALSE
              AND (
                  (t.trip_type = 'OneTime' AND DATE(t.departure_time) = @date)
                  OR
                  (t.trip_type = 'Daily' AND 
                   t.start_date <= @date AND 
                   (t.end_date IS NULL OR t.end_date >= @date) AND
                   (t.days_of_week IS NULL OR t.days_of_week ILIKE @day_pattern))
              )
              AND (LOWER(r.source) LIKE LOWER(@source_pattern) OR LOWER(r.source) = LOWER(@source))
              AND (LOWER(r.destination) LIKE LOWER(@dest_pattern) OR LOWER(r.destination) = LOWER(@destination))
            ORDER BY t.departure_time ASC;", connection);

        var sourcePattern = $"%{source.ToLower()}%";
        var destPattern = $"%{destination.ToLower()}%";
        var dayOfWeek = date.DayOfWeek.ToString().Substring(0, 3); // "Mon", "Tue"...
        var dayPattern = $"%{dayOfWeek}%";
        
        command.Parameters.AddWithValue("date", date);
        command.Parameters.AddWithValue("day_pattern", dayPattern);
        command.Parameters.AddWithValue("source", source.ToLower());
        command.Parameters.AddWithValue("source_pattern", sourcePattern);
        command.Parameters.AddWithValue("destination", destination.ToLower());
        command.Parameters.AddWithValue("dest_pattern", destPattern);

        var rows = new List<(Guid TripId, Guid BusId, Guid OperatorId, Guid RouteId, string BusName, string Source, string Destination, DateTime DepartureTime, DateTime ArrivalTime, int Capacity, decimal BasePrice, decimal PlatformFee, bool IsVariablePrice, string TripType, string? DaysOfWeek, DateOnly? StartDate, DateOnly? EndDate, int ArrivalDayOffset)>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var tripTypeStr = reader.GetString(reader.GetOrdinal("trip_type"));
                DateTime depTime;
                DateTime arrTime;
                int offset = reader.GetInt32(reader.GetOrdinal("arrival_day_offset"));
                DateOnly? sDate = reader.IsDBNull(reader.GetOrdinal("start_date")) ? null : reader.GetFieldValue<DateOnly>(reader.GetOrdinal("start_date"));
                DateOnly? eDate = reader.IsDBNull(reader.GetOrdinal("end_date")) ? null : reader.GetFieldValue<DateOnly>(reader.GetOrdinal("end_date"));

                if (tripTypeStr == "Daily")
                {
                    // Construct times from travel date + stored TimeOnly
                    var dTimeOnly = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("departure_time_only"));
                    var aTimeOnly = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("arrival_time_only"));
                    
                    depTime = date.ToDateTime(dTimeOnly);
                    arrTime = date.AddDays(offset).ToDateTime(aTimeOnly);
                }
                else
                {
                    depTime = reader.GetDateTime(reader.GetOrdinal("departure_time"));
                    arrTime = reader.GetDateTime(reader.GetOrdinal("arrival_time"));
                }

                rows.Add((
                    reader.GetGuid(reader.GetOrdinal("id")),
                    reader.GetGuid(reader.GetOrdinal("bus_id")),
                    reader.GetGuid(reader.GetOrdinal("operator_id")),
                    reader.GetGuid(reader.GetOrdinal("route_id")),
                    reader.GetString(reader.GetOrdinal("bus_name")),
                    reader.GetString(reader.GetOrdinal("source")),
                    reader.GetString(reader.GetOrdinal("destination")),
                    depTime,
                    arrTime,
                    reader.GetInt32(reader.GetOrdinal("capacity")),
                    reader.GetDecimal(reader.GetOrdinal("base_price")),
                    reader.GetDecimal(reader.GetOrdinal("platform_fee")),
                    reader.GetBoolean(reader.GetOrdinal("is_variable_price")),
                    tripTypeStr,
                    reader.IsDBNull(reader.GetOrdinal("days_of_week")) ? null : reader.GetString(reader.GetOrdinal("days_of_week")),
                    sDate,
                    eDate,
                    offset
                ));
            }
        }

        var trips = new List<TripSummary>(rows.Count);
        foreach (var row in rows)
        {
            var reserved = GetReservedSeatCount(connection, row.TripId, date);
            var trip = new TripSummary
            {
                TripId = row.TripId,
                BusId = row.BusId,
                BusName = row.BusName,
                Source = row.Source,
                Destination = row.Destination,
                DepartureTime = row.DepartureTime,
                ArrivalTime = row.ArrivalTime,
                Capacity = row.Capacity,
                SeatsAvailable = row.Capacity - reserved,
                BasePrice = row.BasePrice,
                PlatformFee = row.PlatformFee,
                IsVariablePrice = row.IsVariablePrice,
                TripType = Enum.Parse<TripType>(row.TripType),
                DaysOfWeek = row.DaysOfWeek,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                ArrivalDayOffset = row.ArrivalDayOffset,
                IsActive = true,
                PickupPoints = GetPickupDropPointsInternal(connection, row.OperatorId, row.RouteId, true).ToList(),
                DropPoints = GetPickupDropPointsInternal(connection, row.OperatorId, row.RouteId, false).ToList()
            };
            trips.Add(trip);
        }

        return trips;
    }

    private (Dictionary<int, (string Gender, string Name)>, HashSet<int>) GetReservedSeatsWithGender(NpgsqlConnection connection, Guid tripId, DateOnly travelDate)
    {
        var booked = new Dictionary<int, (string, string)>();
        var locked = new HashSet<int>();

        using (var command = new NpgsqlCommand(@"
            SELECT bp.seat_number, bp.gender, bp.name
            FROM booking_passengers bp
            JOIN bookings b ON b.id = bp.booking_id
            WHERE b.trip_id = @trip_id AND b.travel_date = @travel_date AND b.is_cancelled = FALSE;", connection))
        {
            command.Parameters.AddWithValue("trip_id", tripId);
            command.Parameters.AddWithValue("travel_date", travelDate);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                booked[reader.GetInt32(0)] = (
                    reader.GetString(1),
                    reader.GetString(2)
                );
            }
        }

        using (var lockCommand = new NpgsqlCommand(@"
            SELECT UNNEST(seat_numbers) as seat_num
            FROM seat_locks
            WHERE trip_id = @trip_id AND travel_date = @travel_date AND expires_at > NOW();", connection))
        {
            lockCommand.Parameters.AddWithValue("trip_id", tripId);
            lockCommand.Parameters.AddWithValue("travel_date", travelDate);
            using var lockReader = lockCommand.ExecuteReader();
            while (lockReader.Read())
            {
                locked.Add(lockReader.GetInt32(0));
            }
        }

        return (booked, locked);
    }

    private int GetReservedSeatCount(NpgsqlConnection connection, Guid tripId, DateOnly travelDate)
    {
        int count = 0;

        using (var command = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM booking_passengers bp
            JOIN bookings b ON b.id = bp.booking_id
            WHERE b.trip_id = @trip_id AND b.travel_date = @travel_date AND b.is_cancelled = FALSE;", connection))
        {
            command.Parameters.AddWithValue("trip_id", tripId);
            command.Parameters.AddWithValue("travel_date", travelDate);
            count += Convert.ToInt32(command.ExecuteScalar());
        }

        using (var lockCommand = new NpgsqlCommand(@"
            SELECT COUNT(DISTINCT seat_num)
            FROM (
                SELECT UNNEST(seat_numbers) AS seat_num
                FROM seat_locks
                WHERE trip_id = @trip_id AND travel_date = @travel_date AND expires_at > NOW()
            ) locked_seats;", connection))
        {
            lockCommand.Parameters.AddWithValue("trip_id", tripId);
            lockCommand.Parameters.AddWithValue("travel_date", travelDate);
            var lockCount = lockCommand.ExecuteScalar();
            if (lockCount != null && lockCount != DBNull.Value)
            {
                count += Convert.ToInt32(lockCount);
            }
        }

        return count;
    }


    public OperatorLoginResponse OperatorLogin(OperatorLoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, company_name, email, approval_status, is_disabled, password_hash
            FROM operators
            WHERE email = @email;", connection);
        command.Parameters.AddWithValue("email", email);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException("Operator not found. Please register first.");
        }

        var operatorId = reader.GetGuid(reader.GetOrdinal("id"));
        var companyName = reader.GetString(reader.GetOrdinal("company_name"));
        var approvalStatus = reader.GetString(reader.GetOrdinal("approval_status"));
        var isDisabled = reader.GetBoolean(reader.GetOrdinal("is_disabled"));
        var passwordHash = reader.GetString(reader.GetOrdinal("password_hash"));
        reader.Close();

        if (approvalStatus != OperatorApprovalStatus.Approved.ToString())
        {
            throw new InvalidOperationException("Operator account not approved by admin yet.");
        }

        if (isDisabled)
        {
            throw new InvalidOperationException("Operator account is disabled.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(email, passwordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Invalid password.");
        }

        var jwtToken = GenerateJwtToken(operatorId, email);

        return new OperatorLoginResponse
        {
            OperatorId = operatorId,
            CompanyName = companyName,
            Email = email,
            JwtToken = jwtToken,
            ApprovalStatus = ParseApprovalStatus(approvalStatus),
            IsDisabled = isDisabled,
            Message = "Login successful"
        };
    }

    public BusWithNumberResponse AddBusWithNumber(Guid operatorId, BusRegistrationRequest request)
    {
        using var connection = OpenConnection();
        
        using (var existsCommand = new NpgsqlCommand(
            "SELECT 1 FROM buses WHERE bus_number = @bus_number;", connection))
        {
            existsCommand.Parameters.AddWithValue("bus_number", request.BusNumber.Trim());
            if (existsCommand.ExecuteScalar() is not null)
            {
                throw new InvalidOperationException("Bus number already exists. Bus numbers must be unique.");
            }
        }

        var busId = Guid.NewGuid();
        using var insertCommand = new NpgsqlCommand(@"
            INSERT INTO buses
            (id, operator_id, bus_name, capacity, bus_number, layout_name, layout_json, is_temporarily_unavailable, is_approved, is_active, created_at)
            VALUES
            (@id, @operator_id, @bus_name, @capacity, @bus_number, @layout_name, @layout_json, FALSE, FALSE, TRUE, @created_at);", connection);

        insertCommand.Parameters.AddWithValue("id", busId);
        insertCommand.Parameters.AddWithValue("operator_id", operatorId);
        insertCommand.Parameters.AddWithValue("bus_name", request.BusName.Trim());
        insertCommand.Parameters.AddWithValue("capacity", request.Capacity);
        insertCommand.Parameters.AddWithValue("bus_number", request.BusNumber.Trim());
        insertCommand.Parameters.AddWithValue("layout_name", request.LayoutName.Trim());
        insertCommand.Parameters.AddWithValue("layout_json", request.LayoutJson ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        insertCommand.ExecuteNonQuery();

        CreateNotification(connection, null, "admin@system.com", "Bus registration pending",
            $"New bus '{request.BusName}' ({request.BusNumber}) registered by operator. Awaiting approval.");

        return new BusWithNumberResponse
        {
            BusId = busId,
            BusNumber = request.BusNumber.Trim(),
            BusName = request.BusName.Trim(),
            Capacity = request.Capacity,
            LayoutName = request.LayoutName.Trim(),
            IsApproved = false,
            IsTemporarilyUnavailable = false,
            IsActive = true
        };
    }

    public IEnumerable<OperatorBookingView> GetOperatorBookings(Guid operatorId, Guid? busId = null)
    {
        using var connection = OpenConnection();
        string sql = @"
            SELECT b.id, b.pnr, b.trip_id, b.seat_numbers, b.total_amount, b.payment_status, b.is_cancelled,
                   t.departure_time, bus.bus_name, bus.bus_number, r.source, r.destination
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            JOIN buses bus ON bus.id = t.bus_id
            JOIN routes r ON r.id = t.route_id
            WHERE t.operator_id = @operator_id";

        if (busId.HasValue)
        {
            sql += " AND bus.id = @bus_id";
        }

        sql += " ORDER BY b.created_at DESC;";

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        if (busId.HasValue)
        {
            command.Parameters.AddWithValue("bus_id", busId.Value);
        }

        using var reader = command.ExecuteReader();
        var bookingsList = new List<OperatorBookingView>();
        var bookingsDict = new Dictionary<Guid, OperatorBookingView>();

        while (reader.Read())
        {
            var bookingId = reader.GetGuid(reader.GetOrdinal("id"));
            if (!bookingsDict.TryGetValue(bookingId, out var booking))
            {
                booking = new OperatorBookingView
                {
                    BookingId = bookingId,
                    Pnr = reader.GetString(reader.GetOrdinal("pnr")),
                    TripId = reader.GetGuid(reader.GetOrdinal("trip_id")),
                    BusName = reader.GetString(reader.GetOrdinal("bus_name")),
                    BusNumber = reader.GetString(reader.GetOrdinal("bus_number")),
                    Route = $"{reader.GetString(reader.GetOrdinal("source"))} → {reader.GetString(reader.GetOrdinal("destination"))}",
                    DepartureTime = reader.GetDateTime(reader.GetOrdinal("departure_time")),
                    SeatNumbers = reader.GetFieldValue<int[]>(reader.GetOrdinal("seat_numbers")).ToList(),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                    PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status")),
                    IsCancelled = reader.GetBoolean(reader.GetOrdinal("is_cancelled"))
                };
                bookingsDict[bookingId] = booking;
                bookingsList.Add(booking);
            }
        }
        reader.Close();

        // Fetch passengers separately to avoid INNER JOIN dropping bookings without passengers
        if (bookingsList.Count > 0)
        {
            var bookingIds = bookingsList.Select(b => b.BookingId).ToArray();
            using var passengerCommand = new NpgsqlCommand(@"
                SELECT booking_id, name, age, gender, seat_number
                FROM booking_passengers
                WHERE booking_id = ANY(@ids);", connection);
            passengerCommand.Parameters.AddWithValue("ids", bookingIds);
            using var passengerReader = passengerCommand.ExecuteReader();
            while (passengerReader.Read())
            {
                var bookingId = passengerReader.GetGuid(0);
                if (bookingsDict.TryGetValue(bookingId, out var booking))
                {
                    booking.Passengers.Add(new BookingPassenger
                    {
                        Name = passengerReader.GetString(1),
                        Age = passengerReader.GetInt32(2),
                        Gender = passengerReader.GetString(3),
                        SeatNumber = passengerReader.GetInt32(4)
                    });
                }
            }
        }

        return bookingsList;
    }

    public OperatorRevenueResponse GetOperatorRevenue(Guid operatorId)
    {
        using var connection = OpenConnection();
        
        decimal totalRevenue = 0, revenuePastMonth = 0, revenueThisMonth = 0;
        int totalBookings = 0, activeTrips = 0;
        
        using (var revenueCommand = new NpgsqlCommand(@"
            SELECT 
                SUM(
                    (CASE WHEN t.is_variable_price = TRUE THEN t.base_price * 1.10 ELSE t.base_price END)
                    * COALESCE(array_length(b.seat_numbers, 1), 0)
                ) as total_revenue,
                COUNT(b.id) as total_bookings
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE t.operator_id = @operator_id AND b.is_cancelled = FALSE;", connection))
        {
            revenueCommand.Parameters.AddWithValue("operator_id", operatorId);
            using var reader = revenueCommand.ExecuteReader();
            if (reader.Read())
            {
                totalRevenue = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                totalBookings = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var pastMonthStart = monthStart.AddMonths(-1);

        using (var monthCommand = new NpgsqlCommand(@"
            SELECT 
                SUM(CASE WHEN b.created_at >= @month_start THEN
                    (CASE WHEN t.is_variable_price = TRUE THEN t.base_price * 1.10 ELSE t.base_price END)
                    * COALESCE(array_length(b.seat_numbers, 1), 0)
                    ELSE 0 END) as this_month,
                SUM(CASE WHEN b.created_at >= @past_month_start AND b.created_at < @month_start THEN
                    (CASE WHEN t.is_variable_price = TRUE THEN t.base_price * 1.10 ELSE t.base_price END)
                    * COALESCE(array_length(b.seat_numbers, 1), 0)
                    ELSE 0 END) as past_month
            FROM bookings b
            JOIN trips t ON t.id = b.trip_id
            WHERE t.operator_id = @operator_id AND b.is_cancelled = FALSE;", connection))
        {
            monthCommand.Parameters.AddWithValue("operator_id", operatorId);
            monthCommand.Parameters.AddWithValue("month_start", monthStart);
            monthCommand.Parameters.AddWithValue("past_month_start", pastMonthStart);
            using var reader = monthCommand.ExecuteReader();
            if (reader.Read())
            {
                revenueThisMonth = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                revenuePastMonth = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            }
        }

        using (var tripsCommand = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM trips
            WHERE operator_id = @operator_id AND is_active = TRUE AND departure_time > @now;", connection))
        {
            tripsCommand.Parameters.AddWithValue("operator_id", operatorId);
            tripsCommand.Parameters.AddWithValue("now", now);
            activeTrips = Convert.ToInt32(tripsCommand.ExecuteScalar());
        }

        using (var busesCommand = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM buses
            WHERE operator_id = @operator_id AND is_active = TRUE;", connection))
        {
            busesCommand.Parameters.AddWithValue("operator_id", operatorId);
            var activeBuses = Convert.ToInt32(busesCommand.ExecuteScalar());
        }

        var busRevenue = new List<BusRevenueDetail>();
        using (var busRevenueCommand = new NpgsqlCommand(@"
            SELECT 
                b.id, b.bus_name, b.bus_number,
                SUM(
                    (CASE WHEN t.is_variable_price = TRUE THEN t.base_price * 1.10 ELSE t.base_price END)
                    * COALESCE(array_length(bk.seat_numbers, 1), 0)
                ) as revenue,
                COUNT(bk.id) as bookings
            FROM buses b
            LEFT JOIN trips t ON t.bus_id = b.id
            LEFT JOIN bookings bk ON bk.trip_id = t.id AND bk.is_cancelled = FALSE
            WHERE b.operator_id = @operator_id
            GROUP BY b.id, b.bus_name, b.bus_number;", connection))
        {
            busRevenueCommand.Parameters.AddWithValue("operator_id", operatorId);
            using var reader = busRevenueCommand.ExecuteReader();
            while (reader.Read())
            {
                busRevenue.Add(new BusRevenueDetail
                {
                    BusId = reader.GetGuid(0),
                    BusName = reader.GetString(1),
                    BusNumber = reader.GetString(2),
                    Revenue = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                    Bookings = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
                });
            }
        }

        return new OperatorRevenueResponse
        {
            OperatorId = operatorId,
            TotalRevenue = totalRevenue,
            RevenuePastMonth = revenuePastMonth,
            RevenueThisMonth = revenueThisMonth,
            TotalBookings = totalBookings,
            ActiveBuses = busRevenue.Count(b => b.Revenue > 0 || b.Bookings > 0),
            ActiveTrips = activeTrips,
            BusRevenue = busRevenue
        };
    }

    public PreferredRouteResponse AddPreferredRoute(Guid operatorId, PreferredRouteRequest request)
    {
        using var connection = OpenConnection();
        
        using (var existsCommand = new NpgsqlCommand(
            "SELECT 1 FROM operator_preferred_routes WHERE operator_id = @operator_id AND route_id = @route_id;", connection))
        {
            existsCommand.Parameters.AddWithValue("operator_id", operatorId);
            existsCommand.Parameters.AddWithValue("route_id", request.RouteId);
            if (existsCommand.ExecuteScalar() is not null)
            {
                throw new InvalidOperationException("Route already in preferred list.");
            }
        }

        using var insertCommand = new NpgsqlCommand(@"
            INSERT INTO operator_preferred_routes (id, operator_id, route_id, created_at)
            VALUES (@id, @operator_id, @route_id, @created_at);", connection);
        insertCommand.Parameters.AddWithValue("id", Guid.NewGuid());
        insertCommand.Parameters.AddWithValue("operator_id", operatorId);
        insertCommand.Parameters.AddWithValue("route_id", request.RouteId);
        insertCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        insertCommand.ExecuteNonQuery();

        var route = GetRoute(connection, request.RouteId);
        return new PreferredRouteResponse
        {
            RouteId = route.RouteId,
            Source = route.Source,
            Destination = route.Destination
        };
    }

    public List<PreferredRouteResponse> GetOperatorPreferredRoutes(Guid operatorId)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT r.id, r.source, r.destination
            FROM routes r
            JOIN operator_preferred_routes opr ON opr.route_id = r.id
            WHERE opr.operator_id = @operator_id;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);

        var routes = new List<PreferredRouteResponse>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                routes.Add(new PreferredRouteResponse
                {
                    RouteId = reader.GetGuid(0),
                    Source = reader.GetString(1),
                    Destination = reader.GetString(2),
                    PickupPoints = new List<PickupDropPointResponse>(),
                    DropPoints = new List<PickupDropPointResponse>()
                });
            }
        }

        if (routes.Count == 0) return routes;

        // Fetch all points for these routes in one go
        using var pointsCommand = new NpgsqlCommand(@"
            SELECT route_id, is_pickup, id, location, address, is_default
            FROM pickup_drop_points
            WHERE operator_id = @operator_id;", connection);
        pointsCommand.Parameters.AddWithValue("operator_id", operatorId);

        using (var reader = pointsCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                var routeId = reader.GetGuid(0);
                var isPickup = reader.GetBoolean(1);
                var route = routes.Find(r => r.RouteId == routeId);
                if (route != null)
                {
                    var point = new PickupDropPointResponse
                    {
                        PointId = reader.GetGuid(2),
                        Location = reader.GetString(3),
                        Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                        IsDefault = reader.GetBoolean(5),
                        IsPickup = isPickup
                    };
                    if (isPickup) route.PickupPoints.Add(point);
                    else route.DropPoints.Add(point);
                }
            }
        }

        return routes;
    }

    public IEnumerable<TripSummary> GetOperatorTrips(Guid operatorId)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT t.id, t.bus_id, b.bus_name, r.source, r.destination, t.departure_time, t.arrival_time,
                   b.capacity, t.base_price, t.platform_fee, t.is_variable_price, t.trip_type, t.days_of_week, t.is_active,
                   t.route_id
            FROM trips t
            JOIN routes r ON r.id = t.route_id
            JOIN buses b ON b.id = t.bus_id
            WHERE t.operator_id = @operator_id
            ORDER BY t.departure_time DESC;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);

        var itemsWithRoute = new List<(TripSummary Trip, Guid RouteId)>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var routeId = reader.GetGuid(14);
                itemsWithRoute.Add((new TripSummary
                {
                    TripId = reader.GetGuid(0),
                    BusId = reader.GetGuid(1),
                    BusName = reader.GetString(2),
                    Source = reader.GetString(3),
                    Destination = reader.GetString(4),
                    DepartureTime = reader.GetDateTime(5),
                    ArrivalTime = reader.GetDateTime(6),
                    Capacity = reader.GetInt32(7),
                    BasePrice = reader.GetDecimal(8),
                    PlatformFee = reader.GetDecimal(9),
                    IsVariablePrice = reader.GetBoolean(10),
                    TripType = Enum.Parse<TripType>(reader.GetString(11)),
                    DaysOfWeek = reader.IsDBNull(12) ? null : reader.GetString(12),
                    IsActive = reader.GetBoolean(13),
                    PickupPoints = new List<PickupDropPointResponse>(),
                    DropPoints = new List<PickupDropPointResponse>()
                }, routeId));
            }
        }

        foreach (var (trip, routeId) in itemsWithRoute)
        {
            trip.PickupPoints = GetPickupDropPointsInternal(connection, operatorId, routeId, true).ToList();
            trip.DropPoints = GetPickupDropPointsInternal(connection, operatorId, routeId, false).ToList();
        }

        var items = itemsWithRoute.Select(x => x.Trip).ToList();
        return items;
    }

    public PickupDropPointResponse AddPickupDropPoint(Guid operatorId, Guid routeId, bool isPickup, PickupDropPointRequest request)
    {
        using var connection = OpenConnection();
        var pointId = Guid.NewGuid();
        
        using var insertCommand = new NpgsqlCommand(@"
            INSERT INTO pickup_drop_points (id, operator_id, route_id, is_pickup, location, address, is_default, created_at)
            VALUES (@id, @operator_id, @route_id, @is_pickup, @location, @address, @is_default, @created_at)
            ON CONFLICT (operator_id, route_id, is_pickup, location) 
            DO UPDATE SET address = EXCLUDED.address, created_at = EXCLUDED.created_at
            RETURNING id;", connection);
        insertCommand.Parameters.AddWithValue("id", pointId);
        insertCommand.Parameters.AddWithValue("operator_id", operatorId);
        insertCommand.Parameters.AddWithValue("route_id", routeId);
        insertCommand.Parameters.AddWithValue("is_pickup", isPickup);
        insertCommand.Parameters.AddWithValue("location", request.Location.Trim());
        insertCommand.Parameters.AddWithValue("address", request.Address ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("is_default", request.IsDefault);
        insertCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        
        var result = insertCommand.ExecuteScalar();
        var actualId = result != null ? (Guid)result : pointId;

        return new PickupDropPointResponse
        {
            PointId = actualId,
            Location = request.Location.Trim(),
            Address = request.Address,
            IsDefault = request.IsDefault,
            IsPickup = isPickup
        };
    }

    public IEnumerable<PickupDropPointResponse> GetPickupDropPoints(Guid operatorId, Guid routeId, bool isPickup)
    {
        using var connection = OpenConnection();
        return GetPickupDropPointsInternal(connection, operatorId, routeId, isPickup);
    }

    private IEnumerable<PickupDropPointResponse> GetPickupDropPointsInternal(NpgsqlConnection connection, Guid operatorId, Guid routeId, bool isPickup)
    {
        using var command = new NpgsqlCommand(@"
            SELECT id, location, address, is_default
            FROM pickup_drop_points
            WHERE operator_id = @operator_id AND route_id = @route_id AND is_pickup = @is_pickup
            ORDER BY created_at ASC;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        command.Parameters.AddWithValue("route_id", routeId);
        command.Parameters.AddWithValue("is_pickup", isPickup);

        using var reader = command.ExecuteReader();
        var points = new List<PickupDropPointResponse>();
        while (reader.Read())
        {
            points.Add(new PickupDropPointResponse
            {
                PointId = reader.GetGuid(0),
                Location = reader.GetString(1),
                Address = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsDefault = reader.GetBoolean(3),
                IsPickup = isPickup
            });
        }
        return points;
    }

    public TripSummary AddTripWithDetails(Guid operatorId, TripCreateRequestWithDetails request)
    {
        DeleteExpiredLocks();

        using var connection = OpenConnection();
        var operatorData = GetOperator(connection, operatorId);
        
        if (operatorData.ApprovalStatus != OperatorApprovalStatus.Approved.ToString())
        {
            throw new InvalidOperationException("Operator not approved.");
        }

        var busRecord = GetBus(connection, request.BusId);
        if (busRecord.OperatorId != operatorId || !busRecord.IsApproved || !busRecord.IsActive)
        {
            throw new InvalidOperationException("Bus not valid or not approved.");
        }

        var route = GetRoute(connection, request.RouteId);

        var tripId = Guid.NewGuid();
        var platformFee = GetCurrentPlatformFeeAmount(connection);
        var tripTypeStr = request.TripType.ToString();

        int arrivalDayOffset = 0;
        DateTime departureDateTime;
        DateTime arrivalDateTime;
        DateOnly? startDate = null;
        DateOnly? endDate = null;
        TimeOnly? depTimeOnly = null;
        TimeOnly? arrTimeOnly = null;

        if (request.TripType == TripType.Daily)
        {
            if (!request.StartDate.HasValue || !request.DepartureTime.HasValue || !request.ArrivalTime.HasValue)
            {
                throw new InvalidOperationException("Daily trips require StartDate, DepartureTime, and ArrivalTime.");
            }

            startDate = request.StartDate.Value;
            endDate = request.EndDate;
            depTimeOnly = request.DepartureTime.Value;
            arrTimeOnly = request.ArrivalTime.Value;

            if (arrTimeOnly < depTimeOnly)
            {
                arrivalDayOffset = 1;
            }

            // For Daily trips, we store the start date's datetime in the legacy columns
            departureDateTime = startDate.Value.ToDateTime(depTimeOnly.Value);
            arrivalDateTime = startDate.Value.AddDays(arrivalDayOffset).ToDateTime(arrTimeOnly.Value);
        }
        else
        {
            if (!request.DepartureDateTime.HasValue || !request.ArrivalDateTime.HasValue)
            {
                throw new InvalidOperationException("OneTime trips require DepartureDateTime and ArrivalDateTime.");
            }
            departureDateTime = request.DepartureDateTime.Value;
            arrivalDateTime = request.ArrivalDateTime.Value;
        }

        using var insertCommand = new NpgsqlCommand(@"
            INSERT INTO trips
            (id, operator_id, bus_id, route_id, departure_time, arrival_time, base_price, platform_fee,
             is_variable_price, pickup_points, drop_points, trip_type, days_of_week, is_active, created_at,
             arrival_day_offset, start_date, end_date, departure_time_only, arrival_time_only)
            VALUES
            (@id, @operator_id, @bus_id, @route_id, @departure_time, @arrival_time, @base_price, @platform_fee,
             FALSE, @pickup_points, @drop_points, @trip_type, @days_of_week, TRUE, @created_at,
             @arrival_day_offset, @start_date, @end_date, @departure_time_only, @arrival_time_only);", connection);

        insertCommand.Parameters.AddWithValue("id", tripId);
        insertCommand.Parameters.AddWithValue("operator_id", operatorId);
        insertCommand.Parameters.AddWithValue("bus_id", request.BusId);
        insertCommand.Parameters.AddWithValue("route_id", request.RouteId);
        insertCommand.Parameters.AddWithValue("departure_time", departureDateTime);
        insertCommand.Parameters.AddWithValue("arrival_time", arrivalDateTime);
        insertCommand.Parameters.AddWithValue("base_price", request.BasePrice);
        insertCommand.Parameters.AddWithValue("platform_fee", platformFee);
        insertCommand.Parameters.AddWithValue("pickup_points", request.PickupPoints ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("drop_points", request.DropPoints ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("trip_type", tripTypeStr);
        insertCommand.Parameters.AddWithValue("days_of_week", request.DaysOfWeek ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        insertCommand.Parameters.AddWithValue("arrival_day_offset", arrivalDayOffset);
        insertCommand.Parameters.AddWithValue("start_date", (object?)startDate ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("end_date", (object?)endDate ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("departure_time_only", (object?)depTimeOnly ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("arrival_time_only", (object?)arrTimeOnly ?? DBNull.Value);
        insertCommand.ExecuteNonQuery();

        return new TripSummary
        {
            TripId = tripId,
            BusId = request.BusId,
            BusName = busRecord.BusName,
            Source = route.Source,
            Destination = route.Destination,
            DepartureTime = departureDateTime,
            ArrivalTime = arrivalDateTime,
            Capacity = busRecord.Capacity,
            SeatsAvailable = busRecord.Capacity,
            BasePrice = request.BasePrice,
            PlatformFee = platformFee,
            IsVariablePrice = false,
            TripType = request.TripType,
            DaysOfWeek = request.DaysOfWeek,
            StartDate = startDate,
            EndDate = endDate,
            ArrivalDayOffset = arrivalDayOffset,
            IsActive = true
        };
    }

    public void RequestBusDisable(Guid operatorId, Guid busId, string reason)
    {
        using var connection = OpenConnection();
        var busRecord = GetBus(connection, busId);
        
        if (busRecord.OperatorId != operatorId)
        {
            throw new InvalidOperationException("Bus does not belong to this operator.");
        }

        CreateNotification(connection, null, "admin@system.com", "Bus disable request",
            $"Operator requested to disable bus '{busRecord.BusName}'. Reason: {reason}");
    }

    public PlatformFeeResponse SetPlatformFee(PlatformFeeRequest request)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var archiveCommand = new NpgsqlCommand(
            "UPDATE platform_fees SET is_active = FALSE WHERE is_active = TRUE;", connection, transaction))
        {
            archiveCommand.ExecuteNonQuery();
        }

        var feeId = Guid.NewGuid();
        using var insertCommand = new NpgsqlCommand(@"
            INSERT INTO platform_fees (id, amount, description, is_active, created_at)
            VALUES (@id, @amount, @description, TRUE, @created_at);", connection, transaction);
        insertCommand.Parameters.AddWithValue("id", feeId);
        insertCommand.Parameters.AddWithValue("amount", request.FeeAmount);
        insertCommand.Parameters.AddWithValue("description", request.Description ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow);
        insertCommand.ExecuteNonQuery();

        // Keep trip-level platform_fee in sync with the active global setting
        // so existing trips immediately reflect updated fee in search and booking.
        using (var updateTripsCommand = new NpgsqlCommand(
            "UPDATE trips SET platform_fee = @platform_fee WHERE is_active = TRUE;", connection, transaction))
        {
            updateTripsCommand.Parameters.AddWithValue("platform_fee", request.FeeAmount);
            updateTripsCommand.ExecuteNonQuery();
        }

        transaction.Commit();

        CreateNotification(connection, null, "admin@system.com", "Platform fee updated",
            $"New platform fee set: {request.FeeAmount}");

        return new PlatformFeeResponse
        {
            FeeId = feeId,
            Amount = request.FeeAmount,
            Description = request.Description ?? string.Empty,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public PlatformFeeResponse GetCurrentPlatformFee()
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            SELECT id, amount, description, created_at
            FROM platform_fees
            WHERE is_active = TRUE
            ORDER BY created_at DESC
            LIMIT 1;", connection);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
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
            FeeId = reader.GetGuid(0),
            Amount = reader.GetDecimal(1),
            Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            UpdatedAt = reader.GetDateTime(3)
        };
    }

    private decimal GetCurrentPlatformFeeAmount(NpgsqlConnection connection)
    {
        using var command = new NpgsqlCommand(@"
            SELECT amount FROM platform_fees
            WHERE is_active = TRUE
            ORDER BY created_at DESC
            LIMIT 1;", connection);
        
        var result = command.ExecuteScalar();
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }

    private string GenerateJwtToken(Guid userId, string email)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiry = timestamp + (3600 * 24);
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{'userId':'{userId}','email':'{email}','exp':{expiry}}}"));
        return $"eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.{payload}.signature";
    }

    private NpgsqlConnection OpenConnection()
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    private sealed record OperatorRecord(Guid Id, string CompanyName, string Email, string ApprovalStatus, bool IsDisabled);

    private sealed record BusRecord(
        Guid Id,
        Guid OperatorId,
        string BusNumber,
        string BusName,
        int Capacity,
        bool IsTemporarilyUnavailable,
        bool IsApproved,
        bool IsActive,
        string LayoutName)
    {
        public BusResponse ToResponse()
        {
            return new BusResponse
            {
                BusId = Id,
                OperatorId = OperatorId,
                BusNumber = BusNumber,
                BusName = BusName,
                Capacity = Capacity,
                IsTemporarilyUnavailable = IsTemporarilyUnavailable,
                IsApproved = IsApproved,
                IsActive = IsActive,
                LayoutName = LayoutName
            };
        }
    }
    public void DeleteTrip(Guid operatorId, Guid tripId)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(@"
            UPDATE trips 
            SET is_active = FALSE 
            WHERE id = @trip_id AND operator_id = @operator_id;", connection);
        
        command.Parameters.AddWithValue("trip_id", tripId);
        command.Parameters.AddWithValue("operator_id", operatorId);

        int affected = command.ExecuteNonQuery();
        if (affected == 0)
        {
            throw new KeyNotFoundException("Trip not found or does not belong to this operator.");
        }
    }
}