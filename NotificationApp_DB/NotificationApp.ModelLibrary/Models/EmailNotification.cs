namespace NotificationApp.ModelLibrary.Models{
    // Email Notification Class ineriting Notification class
    public class EmailNotification:Notification{
        public EmailNotification(){
            NotificationType=NotiType.EmailNotification;
        }

        public EmailNotification(string message,DateTime sentDate){
            Message=message;
            SentDate=sentDate;
            NotificationType=NotiType.EmailNotification;
        }
    }
}