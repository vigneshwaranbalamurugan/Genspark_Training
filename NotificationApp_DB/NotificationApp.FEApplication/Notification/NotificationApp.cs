using NotificationApp.BALLibrary.Interfaces;
using NotificationApp.ModelLibrary.Models;
using NotificationApp.FEApplication.Validators;

namespace NotificationApp.FEApplication
{
    public class NotificationApp
    {

        private INotificationService notificationService;
        private  MobileNumberValidator mobileValidator;
        private  EmailValidator emailValidator;
        private  MessageValidator messageValidator;

        public NotificationApp(INotificationService notificationService)
        {
            this.notificationService = notificationService;
            mobileValidator = new MobileNumberValidator();
            emailValidator = new EmailValidator();
            messageValidator = new MessageValidator();
        }

        public void SendNotification()
        {
            Console.WriteLine("Select Notification Type:\n1. SMS\n2. Email");
            int notificationType;
            while (true){
            try
            {
                notificationType = int.Parse(Console.ReadLine() ?? "0");
                break;
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid input. Please enter a number.");
                Console.WriteLine("\n1. SMS\n2. Email");
                continue;
            }
            }
            while (notificationType != 1 && notificationType != 2)
            {
                Console.WriteLine("Invalid choice. Please select 1 for SMS or 2 for Email.");
                notificationType = int.Parse(Console.ReadLine() ?? "0");
            }

            string contactInfo = "";

            if (notificationType == 1)
            {
                contactInfo = mobileValidator.GetAndValidateMobileNumber("Enter Mobile Number of the recipient:");
            }
            if (notificationType == 2)
            {
                contactInfo = emailValidator.GetAndValidateEmail("Enter Email ID of the recipient:");
            }
            string message = messageValidator.GetAndValidateMessage("Enter the message to send:");
            notificationService.CreateNotification(message, notificationType, contactInfo);
        }

        public void GetAllNotifications()
        {
            List<Notification> notifications = notificationService.GetAllNotifications();
            if (notifications == null || notifications.Count == 0)
            {
                Console.WriteLine("No notifications found.");
                return;
            }
            Console.WriteLine("{0,-4} {1,-60} {2,-20} {3}", "ID", "Message & Contact", "Sent Date", "Type");
            Console.WriteLine(new string('-', 100));
            foreach (var notification in notifications)
            {
                var message = notification.Message ?? string.Empty;
                Console.WriteLine("{0,-4} {1,-60} {2,-20} {3}", notification.Id, message, notification.SentDate.ToString("yyyy-MM-dd HH:mm"), notification.NotificationType);
            }
        }
    }
}
