using MySqlConnector;

namespace BlazorApp.Accounts;

public sealed record FavoriteLocation(ulong Id, string Name, decimal Latitude, decimal Longitude, string? Country, string? Region);

public sealed class FavoriteRepository(IConfiguration configuration)
{
    private MySqlConnection Connection() => new(configuration.GetConnectionString("UsersDatabase")
        ?? throw new InvalidOperationException("UsersDatabase is not configured."));

    public async Task<List<FavoriteLocation>> ListAsync(ulong userId)
    {
        await using var connection = Connection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT l.id, l.name, l.latitude, l.longitude, l.country, l.region FROM locations l JOIN favorites f ON f.location_id = l.id WHERE f.user_id = @user ORDER BY l.name";
        command.Parameters.AddWithValue("@user", userId);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<FavoriteLocation>();
        while (await reader.ReadAsync())
            result.Add(new(reader.GetUInt64(0), reader.GetString(1), reader.GetDecimal(2), reader.GetDecimal(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
        return result;
    }

    public async Task<bool> ExistsAsync(ulong userId, double latitude, double longitude)
    {
        await using var connection = Connection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM favorites f JOIN locations l ON l.id = f.location_id WHERE f.user_id = @user AND l.latitude = @lat AND l.longitude = @lon)";
        command.Parameters.AddWithValue("@user", userId);
        // Match the precision used when saving a location.
        command.Parameters.AddWithValue("@lat", Math.Round((decimal)latitude, 5));
        command.Parameters.AddWithValue("@lon", Math.Round((decimal)longitude, 5));
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    public async Task SaveAsync(ulong userId, string name, double latitude, double longitude, string? country, string? region)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || country?.Length > 200 || region?.Length > 200
            || !double.IsFinite(latitude) || !double.IsFinite(longitude) || latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new ArgumentException("Invalid location.");
        await using var connection = Connection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // Unique coordinates let users share a location, also during simultaneous saves.
        command.CommandText = "INSERT INTO locations (name, latitude, longitude, country, region) VALUES (@name, @lat, @lon, @country, @region) ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id)";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@lat", Math.Round((decimal)latitude, 5));
        command.Parameters.AddWithValue("@lon", Math.Round((decimal)longitude, 5));
        command.Parameters.AddWithValue("@country", (object?)country ?? DBNull.Value);
        command.Parameters.AddWithValue("@region", (object?)region ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
        command.CommandText = "SELECT LAST_INSERT_ID()";
        var locationId = Convert.ToUInt64(await command.ExecuteScalarAsync());
        command.CommandText = "INSERT INTO favorites (user_id, location_id) VALUES (@user, @location) ON DUPLICATE KEY UPDATE location_id = @location";
        command.Parameters.AddWithValue("@user", userId);
        command.Parameters.AddWithValue("@location", locationId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    public async Task RemoveAsync(ulong userId, ulong locationId)
    {
        await using var connection = Connection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM favorites WHERE user_id = @user AND location_id = @location";
        command.Parameters.AddWithValue("@user", userId);
        command.Parameters.AddWithValue("@location", locationId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveByCoordinatesAsync(ulong userId, double latitude, double longitude)
    {
        await using var connection = Connection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE f FROM favorites f JOIN locations l ON l.id = f.location_id WHERE f.user_id = @user AND l.latitude = @lat AND l.longitude = @lon";
        command.Parameters.AddWithValue("@user", userId);
        command.Parameters.AddWithValue("@lat", Math.Round((decimal)latitude, 5));
        command.Parameters.AddWithValue("@lon", Math.Round((decimal)longitude, 5));
        await command.ExecuteNonQueryAsync();
    }
}
