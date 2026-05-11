namespace NotificationApp.DALLibrary.Queries{
    public class NotificationQueries{
        public const string InsertNotificationQuery = "INSERT INTO notifications (user_id, message, sent_date, notification_type) VALUES (@userId, @message, @sentDate, @notificationType) RETURNING id";
        public const string GetNotificationByIdQuery = "SELECT id, user_id, message, sent_date, notification_type FROM notifications WHERE id = @key";
        public const string GetAllNotificationsQuery = "SELECT id, user_id, message, sent_date, notification_type FROM notifications";
        public const string UpdateNotificationQuery = "UPDATE notifications SET user_id = @userId, message = @message, sent_date = @sentDate, notification_type = @notificationType WHERE id = @key";
        public const string DeleteNotificationQuery = "DELETE FROM notifications WHERE id = @key";

    }
}