using NotificationApp.DALLibrary.Queries;
using NotificationApp.DALLibrary.Context;
using NotificationApp.ModelLibrary.Models;
using Npgsql;

namespace NotificationApp.DALLibrary.Repositories{
    public class UserRepository: AbstractRepository<int, User>{
        public UserRepository(DbConnection connection) : base(connection)
        {
        }

        public override User Create(User user){
            NpgsqlCommand command = new NpgsqlCommand(UserQueries.InsertUserQuery, _connection);
            command.Parameters.AddWithValue("@userName", user.UserName);
            command.Parameters.AddWithValue("@mobileNumber", user.MobileNumber);
            command.Parameters.AddWithValue("@emailId", user.EmailId);
            try{
                _connection.Open();
                object? result = command.ExecuteScalar();
                if (result != null) user.UserId = Convert.ToInt32(result);
                return user;
            }catch(Exception ex){
                Console.WriteLine(ex.Message);
                return null;
            }
            finally{
                _connection?.Close();
            }

        }

        public override User? Get(int key)
        {
            NpgsqlCommand command = new NpgsqlCommand(UserQueries.GetUserByIdQuery, _connection);
            command.Parameters.AddWithValue("@key", key);
            try
            {
                _connection.Open();
                NpgsqlDataReader reader = command.ExecuteReader();
                if(reader.Read())
                {
                    return new User(Convert.ToInt32(reader[0]), reader[1].ToString()!, reader[2].ToString()!, reader[3].ToString()!);
                }
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally{
                _connection?.Close();
            }
            return null;
        }

        public override List<User>? GetAll()
        {
            NpgsqlCommand command = new NpgsqlCommand(UserQueries.GetAllUsersQuery, _connection);
            List<User> users = new List<User>();
            try
            {
                _connection.Open();
                NpgsqlDataReader reader = command.ExecuteReader();
                while(reader.Read())
                {
                    users.Add(new User(Convert.ToInt32(reader[0]), reader[1].ToString()!, reader[2].ToString()!, reader[3].ToString()!));
                }
                return users;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally{
                _connection?.Close();
            }
        }

        public override User? Update(int key, User item)
        {
            NpgsqlCommand command = new NpgsqlCommand(UserQueries.UpdateUserQuery, _connection);
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@userName", item.UserName);
            command.Parameters.AddWithValue("@mobileNumber", item.MobileNumber);
            command.Parameters.AddWithValue("@emailId", item.EmailId);
            try
            {
                _connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                if(rowsAffected > 0)
                {
                    return item;
                }
                return null;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally{
                _connection?.Close();
            }
        }

        public override User? Delete(int key)
        {
            NpgsqlCommand command = new NpgsqlCommand(UserQueries.DeleteUserQuery, _connection);
            command.Parameters.AddWithValue("@key", key);
            try
            {
                _connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                if(rowsAffected > 0)
                {
                    return new User(key, "", "", "");
                }
                return null;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally{
                _connection?.Close();
            }
        }

        public  User? GetByEmail(string email)
        {
            NpgsqlCommand command = new NpgsqlCommand(UserQueries.GetUserByEmailQuery, _connection);
            command.Parameters.AddWithValue("@email", email);
            try
            {
                _connection.Open();
                NpgsqlDataReader reader = command.ExecuteReader();
                if(reader.Read())
                {
                    return new User(Convert.ToInt32(reader[0]), reader[1].ToString()!, reader[2].ToString()!, reader[3].ToString()!);
                }
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally{
                _connection?.Close();
            }
            return null;
        }

        public User? GetByMobileNumber(string mobileNumber)
        {
            NpgsqlCommand command = new NpgsqlCommand(UserQueries.GetUserByMobileQuery, _connection);
            command.Parameters.AddWithValue("@mobile", mobileNumber);
            try
            {
                _connection.Open();
                NpgsqlDataReader reader = command.ExecuteReader();
                if(reader.Read())
                {
                    return new User(Convert.ToInt32(reader[0]), reader[1].ToString()!, reader[2].ToString()!, reader[3].ToString()!);
                }
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally{
                _connection?.Close();
            }
            return null;
        }


    }
}