namespace NotificationApp.ModelLibrary.Models{

    // SMS Notification Class ineriting Notification class
    public class SMSNotification:Notification{
        public SMSNotification(){
            NotificationType=NotiType.SMSNotification;
        }
        public SMSNotification(string message,DateTime sentDate){
            Message=message;
            SentDate=sentDate;
            NotificationType=NotiType.SMSNotification;
        }
    }
}