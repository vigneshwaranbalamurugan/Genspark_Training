using System;
using NotificationSystem.Interfaces;
using NotificationSystem.Models;
using NotificationSystem.Services;

namespace NotificationSystem{
    internal class Program{
        private readonly IUserInteract userInteract;
        private readonly NotificationService notificationService;

        public Program(){
            userInteract = new UserService();
            notificationService = new NotificationService();
        }

        public void Run(){
            int choice=0;
            while(choice!=3){
               Console.WriteLine("Select the option:\n1. Create User\n2. Send Notification\n3. Exit");
               choice = int.Parse(Console.ReadLine() ?? "0");
               switch(choice){
                    case 1:
                        User user = userInteract.CreateUser();
                        Console.WriteLine($"User created with ID: {user.UserId}");
                        break;
                    case 2:
                        Console.WriteLine("Select Notification Type:\n1. SMS\n2. Email");
                        int notificationType = int.Parse(Console.ReadLine() ?? "0");
                        while(notificationType!=1 && notificationType!=2){
                            Console.WriteLine("Invalid choice. Please select 1 for SMS or 2 for Email.");
                            notificationType = int.Parse(Console.ReadLine() ?? "0");
                        }
                        User userToNotify = null;
                        if(notificationType==1){
                            Console.WriteLine("Enter Mobile Number:");
                            string mobileNumber = Console.ReadLine() ?? "";  
                            userToNotify = userInteract.getUserByMobileNumber(mobileNumber);                            
                        }
                        if(notificationType==2){
                            Console.WriteLine("Enter Email ID:");
                            string emailId = Console.ReadLine() ?? "";
                            userToNotify = userInteract.getUserByEmail(emailId);
                        }
                        if(userToNotify==null){
                            Console.WriteLine("User not found. Please create the user first.");
                            break;
                        }
                        Console.WriteLine("Enter the message:");
                        string message = Console.ReadLine() ?? "";
                        notificationService.Send(userToNotify, message, notificationType);
                        break;
                    case 3:
                        Console.WriteLine("Exiting...");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
               }
            }
        }

        static void Main(string[] args){
            new Program().Run();
        }
    }
}