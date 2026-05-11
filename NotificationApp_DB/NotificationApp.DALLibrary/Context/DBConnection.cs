using Npgsql;

namespace NotificationApp.DALLibrary.Context
{
    public class DbConnection : IDisposable
    {
        private readonly string _connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=978681;Database=notificationappdb";
        
        private NpgsqlConnection? _connection;

        public NpgsqlConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new NpgsqlConnection(_connectionString);
            }
            return _connection;
        }

        public void Open()
        {
            GetConnection().Open();
        }

        public void Close()
        {
            _connection?.Close();
        }

        public static implicit operator NpgsqlConnection(DbConnection connection)
        {
            return connection.GetConnection();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}