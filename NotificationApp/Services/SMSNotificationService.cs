using NotificationSystem.Interfaces;
using NotificationSystem.Models;

namespace NotificationSystem.Services{
    // SMS Notification Service implementing INotification interface
    internal class SMSNotificationService:INotification{
        public void SendNotification(User user,Notification notification){
            Console.WriteLine($"SMS sent to {user.UserName} with message: {notification.Message}");
        }
    }
}