using Npgsql;
using System.Net.NetworkInformation;

namespace UnderstandingADOApp
{
    
    internal class Program
    {
        string connectionString =
            "Host=localhost;Port=5432;Database=dummydb;Username=postgres;Password=978681";

        public Program()
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                connection.Open();
                Console.WriteLine("Connected to PostgreSQL successfully!");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            new Program();

        }
    }
}
