using System;

namespace NotificationSystem.Models{
    public enum NotiType{
        EmailNotification = 1, SMSNotification =2
    }

    // Base Notification Class
    internal class Notification{

        public string Message{set;get;}=string.Empty;
        public DateTime SentDate{get;set;}
        public NotiType NotificationType{set;get;}

        public Notification(){
        }

        public Notification(string message,DateTime sentDate){
            Message=message;
            SentDate=sentDate;
        }
    }
}