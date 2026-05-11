namespace NotificationApp.ModelLibrary.Models{
    public enum NotiType{
        EmailNotification = 1, SMSNotification =2
    }

    // Base Notification Class
    public  class Notification{

        public int Id{get;set;}
        public string Message{set;get;}=string.Empty;
        public int UsertoNotify{set;get;}
        public DateTime SentDate{get;set;}
        public NotiType NotificationType{set;get;}

        public Notification(){
        }

        public Notification(string message,DateTime sentDate){
            Message=message;
            SentDate=sentDate;
        }

        public override string ToString(){
            return $"Notification Id: {Id}, Message: {Message}, User to Notify: {UsertoNotify}, Sent Date: {SentDate}, Notification Type: {NotificationType}";
        }
    }
}