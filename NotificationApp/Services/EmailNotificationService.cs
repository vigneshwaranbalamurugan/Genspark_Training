using NotificationSystem.Interfaces;
using NotificationSystem.Models;

namespace NotificationSystem.Services{
    internal class EmailNotificationService:INotification{
        public void SendNotification(User user,Notification notification){
            Console.WriteLine($"Email sent to {user.UserName} with message: {notification.Message}");
        }
    }
}