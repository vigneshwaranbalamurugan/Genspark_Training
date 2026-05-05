using NotificationSystem.Models;

namespace NotificationSystem.Interfaces{

    // Notification Interface
    internal interface INotification{
        void SendNotification(User user,Notification notification);
    }
}

