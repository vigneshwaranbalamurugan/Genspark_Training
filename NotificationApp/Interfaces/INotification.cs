using NotificationSystem.Models;

namespace NotificationSystem.Interfaces{
    internal interface INotification{
        void SendNotification(User user,Notification notification);
    }
}

