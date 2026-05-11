using NotificationApp.BALLibrary.Interfaces;
using NotificationApp.ModelLibrary.Models;

namespace NotificationApp.BALLibrary.Services{
    // SMS Notification Service implementing INotification interface
    internal class SMSNotificationSender:INotificationSender{
        public void SendNotification(User user,Notification notification){
            Console.WriteLine($"SMS sent to {user.UserName} with message: {notification.Message}");
        }
    }
}