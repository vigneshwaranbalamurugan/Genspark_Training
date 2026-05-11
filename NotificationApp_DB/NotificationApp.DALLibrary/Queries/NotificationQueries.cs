namespace NotificationApp.DALLibrary.Queries{
    public class NotificationQueries{
        public const string InsertNotificationQuery = "INSERT INTO notifications (user_id, message, sent_date, notification_type) VALUES (@userId, @message, @sentDate, @notificationType) RETURNING id";
        public const string GetNotificationByIdQuery = "SELECT n.id, n.user_id, n.message, n.sent_date, n.notification_type, u.email_id, u.mobile_number FROM notifications n JOIN users u ON n.user_id = u.user_id WHERE n.id = @key";
        public const string GetAllNotificationsQuery = "SELECT n.id, n.user_id, n.message, n.sent_date, n.notification_type, u.email_id, u.mobile_number FROM notifications n JOIN users u ON n.user_id = u.user_id";
        public const string UpdateNotificationQuery = "UPDATE notifications SET user_id = @userId, message = @message, sent_date = @sentDate, notification_type = @notificationType WHERE id = @key";
        public const string DeleteNotificationQuery = "DELETE FROM notifications WHERE id = @key";

    }
}