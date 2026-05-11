using NotificationApp.BALLibrary.Interfaces;
using NotificationApp.ModelLibrary.Models;
using NotificationApp.DALLibrary.Repositories;

namespace NotificationApp.BALLibrary.Services{
    // Notification Service to send notification to user based on notification type
    
    public class NotificationService:INotificationService{
        
        private IUserInteract userInteract;
        private NotificationRepository notificationRepository;
        
        public NotificationService(IUserInteract userInteract, NotificationRepository notificationRepository){
            this.userInteract = userInteract;
            this.notificationRepository = notificationRepository;
        }

        public void Send(User userToNotify,Notification notification){
            INotificationSender notify;
            if(notification.NotificationType==NotiType.EmailNotification){
                notify=new EmailNotificationSender();
            }else{
                notify=new SMSNotificationSender();
            }
            notify.SendNotification(userToNotify, notification);
        }

        public void CreateNotification(string message, int notificationType,string contactInfo){
            User user;
            Notification notification;

            if (notificationType == 1){
                user=userInteract.getUserByMobileNumber(contactInfo);
                if(user==null){
                    Console.WriteLine("User not found. Please create the user first.");
                    return;
                }
                notification= new SMSNotification{
                    Message=message,
                    NotificationType=NotiType.SMSNotification,
                    UsertoNotify=user.UserId,
                    SentDate=DateTime.Now
                };
            }else{
                user = userInteract.getUserByEmail(contactInfo); 
                if(user==null){
                    Console.WriteLine("User not found. Please create the user first.");
                    return;
                }
                notification = new EmailNotification{
                    Message = message,
                    NotificationType = NotiType.EmailNotification,
                    UsertoNotify = user.UserId,
                    SentDate = DateTime.Now
                };
            } 
            notificationRepository.Create(notification);
            Send(user, notification);
        }

        public Notification? GetById(int id){
            return notificationRepository.Get(id);
        }

        // Get All Notifications
        public List<Notification> GetAllNotifications(){
            return notificationRepository.GetAll();
        }
            
    }
}