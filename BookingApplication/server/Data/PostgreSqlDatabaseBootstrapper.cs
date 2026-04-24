using Npgsql;

namespace server.Data;

public static class PostgreSqlDatabaseBootstrapper
{
    public static void EnsureDatabaseExists(string connectionString)
    {
        var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(connectionBuilder.Database))
        {
            throw new InvalidOperationException("The PostgreSql connection string must include a database name.");
        }

        var targetDatabase = connectionBuilder.Database;
        connectionBuilder.Database = "postgres";

        using var connection = new NpgsqlConnection(connectionBuilder.ConnectionString);
        connection.Open();

        using var existsCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @database_name;", connection);
        existsCommand.Parameters.AddWithValue("database_name", targetDatabase);
        var exists = existsCommand.ExecuteScalar() is not null;
        if (exists)
        {
            return;
        }

        var quotedDatabaseName = QuoteIdentifier(targetDatabase);
        using var createCommand = new NpgsqlCommand($"CREATE DATABASE {quotedDatabaseName};", connection);
        createCommand.ExecuteNonQuery();
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}