using NotificationSystem.Interfaces;
using NotificationSystem.Models;

namespace NotificationSystem.Services{
    // Notification Service to send notification to user based on notification type
    internal class NotificationService{
        
        public void Send(User userToNotify,string message,int notificationType){
            INotification notify;
            Notification notification;
            if(notificationType == 1){
                
                notification = new SMSNotification(message, DateTime.Now);
                notify = new SMSNotificationService();
            }else{
                notification = new EmailNotification(message, DateTime.Now);
                notify = new EmailNotificationService();
            }
            notify.SendNotification(userToNotify, notification);
        }
    }
}