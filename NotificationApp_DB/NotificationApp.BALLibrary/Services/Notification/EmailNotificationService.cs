using NotificationApp.BALLibrary.Interfaces;
using NotificationApp.ModelLibrary.Models;

namespace NotificationApp.BALLibrary.Services{
    // Email Notification Service implementing INotification interface
    internal class EmailNotificationSender:INotificationSender{
        public void SendNotification(User user,Notification notification){
            Console.WriteLine($"Email sent to {user.UserName} with message: {notification.Message}");
        }
    }
}