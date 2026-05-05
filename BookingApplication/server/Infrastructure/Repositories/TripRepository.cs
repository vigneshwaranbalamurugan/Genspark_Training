using Npgsql;
using server.Domain.Entities;
using server.Domain.Interfaces;
using server.Infrastructure.Data;

namespace server.Infrastructure.Repositories;

public sealed class TripRepository : ITripRepository
{
    private readonly DbConnectionFactory _factory;

    public TripRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<TripEntity?> GetByIdAsync(Guid tripId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_id, route_id, departure_time, arrival_time,
                   base_price, platform_fee, is_variable_price, pickup_points, drop_points,
                   trip_type, days_of_week, is_active, arrival_day_offset,
                   start_date, end_date, departure_time_only, arrival_time_only
            FROM trips WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", tripId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapEntity(reader);
    }

    public async Task<IEnumerable<TripEntity>> GetByOperatorIdAsync(Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT id, operator_id, bus_id, route_id, departure_time, arrival_time,
                   base_price, platform_fee, is_variable_price, pickup_points, drop_points,
                   trip_type, days_of_week, is_active, arrival_day_offset,
                   start_date, end_date, departure_time_only, arrival_time_only
            FROM trips
            WHERE operator_id = @operator_id
            ORDER BY departure_time DESC;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<TripEntity>();
        while (await reader.ReadAsync()) results.Add(MapEntity(reader));
        return results;
    }

    public async Task<IEnumerable<TripDetail>> GetTripsWithDetailsByOperatorAsync(Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT t.id, t.operator_id, t.bus_id, t.route_id, t.departure_time, t.arrival_time,
                   t.base_price, t.platform_fee, t.is_variable_price, t.pickup_points, t.drop_points,
                   t.trip_type, t.days_of_week, t.is_active, t.arrival_day_offset,
                   t.start_date, t.end_date, t.departure_time_only, t.arrival_time_only,
                   b.bus_name, b.bus_number, b.capacity, b.layout_name, b.layout_json,
                   r.source, r.destination, o.company_name
            FROM trips t
            JOIN routes r ON r.id = t.route_id
            JOIN buses b ON b.id = t.bus_id
            JOIN operators o ON o.id = t.operator_id
            WHERE t.operator_id = @operator_id
            ORDER BY t.departure_time DESC;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);

        return await ReadTripDetailsAsync(command);
    }

    public async Task<TripEntity> CreateAsync(TripEntity entity)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            INSERT INTO trips
            (id, operator_id, bus_id, route_id, departure_time, arrival_time, base_price, platform_fee,
             is_variable_price, pickup_points, drop_points, trip_type, days_of_week, is_active, created_at,
             arrival_day_offset, start_date, end_date, departure_time_only, arrival_time_only)
            VALUES
            (@id, @operator_id, @bus_id, @route_id, @departure_time, @arrival_time, @base_price, @platform_fee,
             @is_variable_price, @pickup_points, @drop_points, @trip_type, @days_of_week, TRUE, @created_at,
             @arrival_day_offset, @start_date, @end_date, @departure_time_only, @arrival_time_only);", connection);

        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("operator_id", entity.OperatorId);
        command.Parameters.AddWithValue("bus_id", entity.BusId);
        command.Parameters.AddWithValue("route_id", entity.RouteId);
        command.Parameters.AddWithValue("departure_time", entity.DepartureTime);
        command.Parameters.AddWithValue("arrival_time", entity.ArrivalTime);
        command.Parameters.AddWithValue("base_price", entity.BasePrice);
        command.Parameters.AddWithValue("platform_fee", entity.PlatformFee);
        command.Parameters.AddWithValue("is_variable_price", entity.IsVariablePrice);
        command.Parameters.AddWithValue("pickup_points", entity.PickupPoints ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("drop_points", entity.DropPoints ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("trip_type", entity.TripType);
        command.Parameters.AddWithValue("days_of_week", entity.DaysOfWeek ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        command.Parameters.AddWithValue("arrival_day_offset", entity.ArrivalDayOffset);
        command.Parameters.AddWithValue("start_date", (object?)entity.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("end_date", (object?)entity.EndDate ?? DBNull.Value);
        command.Parameters.AddWithValue("departure_time_only", (object?)entity.DepartureTimeOnly ?? DBNull.Value);
        command.Parameters.AddWithValue("arrival_time_only", (object?)entity.ArrivalTimeOnly ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
        return entity;
    }

    public async Task DeactivateAsync(Guid tripId, Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE trips SET is_active = FALSE
            WHERE id = @trip_id AND operator_id = @operator_id;", connection);
        command.Parameters.AddWithValue("trip_id", tripId);
        command.Parameters.AddWithValue("operator_id", operatorId);
        var affected = await command.ExecuteNonQueryAsync();
        if (affected == 0)
            throw new KeyNotFoundException("Trip not found or does not belong to this operator.");
    }

    public async Task DeactivateByOperatorAsync(Guid operatorId, DateTime cutoff)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE trips SET is_active = FALSE
            WHERE operator_id = @operator_id AND departure_time > @now;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        command.Parameters.AddWithValue("now", cutoff);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePlatformFeeForActiveTripsAsync(decimal newFee)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            UPDATE trips SET platform_fee = @platform_fee WHERE is_active = TRUE;", connection);
        command.Parameters.AddWithValue("platform_fee", newFee);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> CountActiveByOperatorAsync(Guid operatorId)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM trips
            WHERE operator_id = @operator_id AND is_active = TRUE AND departure_time > @now;", connection);
        command.Parameters.AddWithValue("operator_id", operatorId);
        command.Parameters.AddWithValue("now", DateTime.UtcNow);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<IEnumerable<TripDetail>> SearchAsync(string source, string destination, DateOnly date)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var command = new NpgsqlCommand(@"
            SELECT t.id, t.operator_id, t.bus_id, t.route_id, t.departure_time, t.arrival_time,
                   t.base_price, t.platform_fee, t.is_variable_price, t.pickup_points, t.drop_points,
                   t.trip_type, t.days_of_week, t.is_active, t.arrival_day_offset,
                   t.start_date, t.end_date, t.departure_time_only, t.arrival_time_only,
                   b.bus_name, b.bus_number, b.capacity, b.layout_name, b.layout_json,
                   r.source, r.destination, o.company_name
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
                  (t.trip_type = 'Daily' AND DATE(t.departure_time) <= @travel_date
                   AND t.days_of_week LIKE '%' || @day_of_week || '%')
              )
            ORDER BY t.departure_time;", connection);

        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("destination", destination);
        command.Parameters.AddWithValue("travel_date", date.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("day_of_week", date.ToString("ddd"));

        return await ReadTripDetailsAsync(command);
    }

    public async Task<IEnumerable<TripDetail>> SearchFuzzyAsync(string source, string destination, DateOnly date)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        var dayOfWeek = date.DayOfWeek.ToString().Substring(0, 3);
        await using var command = new NpgsqlCommand(@"
            SELECT t.id, t.operator_id, t.bus_id, t.route_id, t.departure_time, t.arrival_time,
                   t.base_price, t.platform_fee, t.is_variable_price, t.pickup_points, t.drop_points,
                   t.trip_type, t.days_of_week, t.is_active, t.arrival_day_offset,
                   t.start_date, t.end_date, t.departure_time_only, t.arrival_time_only,
                   b.bus_name, b.bus_number, b.capacity, b.layout_name, b.layout_json,
                   r.source, r.destination, o.company_name
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

        command.Parameters.AddWithValue("date", date);
        command.Parameters.AddWithValue("day_pattern", $"%{dayOfWeek}%");
        command.Parameters.AddWithValue("source", source.ToLower());
        command.Parameters.AddWithValue("source_pattern", $"%{source.ToLower()}%");
        command.Parameters.AddWithValue("destination", destination.ToLower());
        command.Parameters.AddWithValue("dest_pattern", $"%{destination.ToLower()}%");

        return await ReadTripDetailsAsync(command);
    }

    private static async Task<List<TripDetail>> ReadTripDetailsAsync(NpgsqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<TripDetail>();
        while (await reader.ReadAsync())
        {
            var trip = MapEntity(reader);
            results.Add(new TripDetail
            {
                Trip = trip,
                BusName = reader.GetString(19),
                BusNumber = reader.IsDBNull(20) ? string.Empty : reader.GetString(20),
                BusCapacity = reader.GetInt32(21),
                LayoutName = reader.GetString(22),
                LayoutJson = reader.IsDBNull(23) ? null : reader.GetString(23),
                Source = reader.GetString(24),
                Destination = reader.GetString(25),
                CompanyName = reader.GetString(26)
            });
        }
        return results;
    }

    private static TripEntity MapEntity(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        OperatorId = r.GetGuid(1),
        BusId = r.GetGuid(2),
        RouteId = r.GetGuid(3),
        DepartureTime = r.GetDateTime(4),
        ArrivalTime = r.GetDateTime(5),
        BasePrice = r.GetDecimal(6),
        PlatformFee = r.GetDecimal(7),
        IsVariablePrice = r.GetBoolean(8),
        PickupPoints = r.IsDBNull(9) ? null : r.GetString(9),
        DropPoints = r.IsDBNull(10) ? null : r.GetString(10),
        TripType = r.GetString(11),
        DaysOfWeek = r.IsDBNull(12) ? null : r.GetString(12),
        IsActive = r.GetBoolean(13),
        ArrivalDayOffset = r.GetInt32(14),
        StartDate = r.IsDBNull(15) ? null : r.GetFieldValue<DateOnly>(15),
        EndDate = r.IsDBNull(16) ? null : r.GetFieldValue<DateOnly>(16),
        DepartureTimeOnly = r.IsDBNull(17) ? null : r.GetFieldValue<TimeOnly>(17),
        ArrivalTimeOnly = r.IsDBNull(18) ? null : r.GetFieldValue<TimeOnly>(18)
    };
}
