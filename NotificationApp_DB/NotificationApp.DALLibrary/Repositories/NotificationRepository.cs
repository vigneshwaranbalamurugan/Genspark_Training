using System;
using NotificationApp.ModelLibrary.Models;
using NotificationApp.DALLibrary.Context;
using NotificationApp.DALLibrary.Queries;
using Npgsql;

namespace NotificationApp.DALLibrary.Repositories
{
    public class NotificationRepository : AbstractRepository<int, Notification>
    {
        public NotificationRepository(DbConnection connection) : base(connection)
        {
        }

        public override Notification Create(Notification item)
        {
            NpgsqlCommand command = new NpgsqlCommand(NotificationQueries.InsertNotificationQuery, _connection);
            command.Parameters.AddWithValue("@userId", item.UsertoNotify);
            command.Parameters.AddWithValue("@message", item.Message);
            command.Parameters.AddWithValue("@sentDate", item.SentDate == default ? DateTime.UtcNow : item.SentDate);
            command.Parameters.AddWithValue("@notificationType", (int)item.NotificationType);
            try
            {
                _connection.Open();
                object? result = command.ExecuteScalar();
                if (result != null)
                {
                    item.Id = Convert.ToInt32(result);
                }
                return item;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                _connection?.Close();
            }
        }

        public override List<Notification>? GetAll()
        {
            NpgsqlCommand command = new NpgsqlCommand(NotificationQueries.GetAllNotificationsQuery, _connection);
            List<Notification> notifications = new List<Notification>();
            try
            {
                _connection.Open();
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var n = new Notification();
                    n.Id = Convert.ToInt32(reader[0]);
                    n.UsertoNotify = Convert.ToInt32(reader[1]);
                    n.Message = reader[2].ToString()!;
                    n.SentDate = Convert.ToDateTime(reader[3]);
                    n.NotificationType = (NotiType)Convert.ToInt32(reader[4]);
                    notifications.Add(n);
                }
                return notifications;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                _connection?.Close();
            }
        }

        public override Notification? Get(int key)
        {
            NpgsqlCommand command = new NpgsqlCommand(NotificationQueries.GetNotificationByIdQuery, _connection);
            command.Parameters.AddWithValue("@key", key);
            try
            {
                _connection.Open();
                NpgsqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var n = new Notification();
                    n.Id = Convert.ToInt32(reader[0]);
                    n.UsertoNotify = Convert.ToInt32(reader[1]);
                    n.Message = reader[2].ToString()!;
                    n.SentDate = Convert.ToDateTime(reader[3]);
                    n.NotificationType = (NotiType)Convert.ToInt32(reader[4]);
                    return n;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                _connection?.Close();
            }
            return null;
        }

        public override Notification? Update(int key, Notification item)
        {
            NpgsqlCommand command = new NpgsqlCommand(NotificationQueries.UpdateNotificationQuery, _connection);
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@userId", item.UsertoNotify);
            command.Parameters.AddWithValue("@message", item.Message);
            command.Parameters.AddWithValue("@sentDate", item.SentDate == default ? DateTime.UtcNow : item.SentDate);
            command.Parameters.AddWithValue("@notificationType", (int)item.NotificationType);
            try
            {
                _connection.Open();
                int rows = command.ExecuteNonQuery();
                if (rows > 0)
                {
                    item.Id = key;
                    return item;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                _connection?.Close();
            }
        }

        public override Notification? Delete(int key)
        {
            NpgsqlCommand command = new NpgsqlCommand(NotificationQueries.DeleteNotificationQuery, _connection);
            command.Parameters.AddWithValue("@key", key);
            try
            {
                _connection.Open();
                int rows = command.ExecuteNonQuery();
                if (rows > 0)
                {
                    var deleted = new Notification();
                    deleted.Id = key;
                    return deleted;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                _connection?.Close();
            }
        }


    }
}