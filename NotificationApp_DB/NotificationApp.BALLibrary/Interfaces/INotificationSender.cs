using NotificationApp.ModelLibrary.Models;

namespace NotificationApp.BALLibrary.Interfaces{

    // Notification Interface
    public interface INotificationSender{
        void SendNotification(User user,Notification notification);
    }

    public interface INotificationService{
        void CreateNotification(string message, int notificationType,string contactInfo);

        void Send(User userToNotify,Notification notification);

        Notification? GetById(int id);

        List<Notification> GetAllNotifications();
    }
}

