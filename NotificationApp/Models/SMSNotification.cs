namespace NotificationSystem.Models{
    internal class SMSNotification:Notification{
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