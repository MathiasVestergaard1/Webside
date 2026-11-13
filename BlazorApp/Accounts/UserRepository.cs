using MySqlConnector;

namespace BlazorApp.Accounts;

public sealed class UserRepository(IConfiguration configuration)
{
    // Opret en forbindelse med indstillingerne fra konfigurationen.
    private MySqlConnection Connection() => new(configuration.GetConnectionString("UsersDatabase")
        ?? throw new InvalidOperationException("UsersDatabase is not configured."));

    // Find en bruger ud fra brugernavn. Returnér null, hvis brugeren ikke findes.
    public async Task<UserAccount?> FindAsync(string username, CancellationToken cancellationToken)
    {
        await using var connection = Connection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // SQL SELECT: Hent brugerens id, navn, e-mail og password-hash.
        command.CommandText = "SELECT id, username, email, password_hash FROM users WHERE username = @username LIMIT 1";
        // Parametre holder brugerens input adskilt fra SQL-koden.
        command.Parameters.AddWithValue("@username", username);
        // Kør SELECT og læs resultatet.
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new(reader.GetUInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3))
            : null;
    }

    // Gem en ny bruger med et hashed password.
    public async Task CreateAsync(string username, string email, string hash, CancellationToken cancellationToken)
    {
        await using var connection = Connection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // SQL INSERT: Tilføj en bruger i users-tabellen.
        command.CommandText = "INSERT INTO users (username, email, password_hash) VALUES (@username, @email, @hash)";
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@hash", hash);
        // Kør INSERT i databasen.
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
