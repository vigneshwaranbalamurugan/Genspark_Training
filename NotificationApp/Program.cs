using System;
using NotificationSystem.Interfaces;
using NotificationSystem.Models;
using NotificationSystem.Services;

namespace NotificationSystem{
    internal class Program{
        IUserInteract userInteract;
        NotificationService notificationService;

        public Program(){
            userInteract = new UserService();
            notificationService = new NotificationService();
        }

        // Method to send notification
        public void Notification(){
            Console.WriteLine("Select Notification Type:\n1. SMS\n2. Email");
            int notificationType = int.Parse(Console.ReadLine() ?? "0");
            while(notificationType!=1 && notificationType!=2){
                Console.WriteLine("Invalid choice. Please select 1 for SMS or 2 for Email.");
                notificationType = int.Parse(Console.ReadLine() ?? "0");
            }
            User userToNotify = null;
            string mobileNumber;

            if(notificationType==1){
                Console.WriteLine("Enter Mobile Number:");
                mobileNumber = Console.ReadLine() ?? "";  
                userToNotify = userInteract.getUserByMobileNumber(mobileNumber);                            
            }
            if(notificationType==2){
                Console.WriteLine("Enter Email ID:");
                string emailId = Console.ReadLine() ?? "";
                userToNotify = userInteract.getUserByEmail(emailId);
            }
            if(userToNotify==null){
                Console.WriteLine("User not found. Please create the user first.");
                return;
            }
            Console.WriteLine("Enter the message:");
            string message = Console.ReadLine() ?? "";
            notificationService.Send(userToNotify, message, notificationType);
        }

        // Main method to run the application
        public void Run(){
            int choice=0;
            string mobileNumber;
            Console.WriteLine("--Welcome to Notification Application--");
            while(choice!=6){
               Console.WriteLine("Select the option:\n1. Create User\n2. Send Notification\n3. Get User Details\n4. Update User Details\n5. Delete User\n6. Exit");
               choice = int.Parse(Console.ReadLine() ?? "0");
               switch(choice){
                    case 1:
                        User newUser = userInteract.CreateUser();
                        Console.WriteLine(newUser);
                        break;
                    case 2:
                        Notification();
                        break;
                    case 3:
                        Console.WriteLine("Enter Mobile Number:");
                        mobileNumber = Console.ReadLine() ?? "";
                        User userDetails = userInteract.getUserByMobileNumber(mobileNumber);
                        if(userDetails!=null){
                            Console.WriteLine(userDetails);
                        }else{
                            Console.WriteLine("User not found.");
                        }
                        break;
                    case 4:
                        Console.WriteLine("Enter Mobile Number:");
                        mobileNumber = Console.ReadLine() ?? "";
                        User updatedUser = userInteract.UpdateUser(mobileNumber);
                        if(updatedUser!=null){
                            Console.WriteLine(updatedUser);
                        }else{
                            Console.WriteLine("User not found.");
                        }
                        break;
                    case 5:
                        Console.WriteLine("Enter Mobile Number:");
                        mobileNumber = Console.ReadLine() ?? "";
                        User deteledUser = userInteract.DeleteUser(mobileNumber);
                        if(deteledUser!=null){
                           Console.WriteLine(deteledUser);
                        }else{
                            Console.WriteLine("User not found.");
                        }
                        break;
                    case 6:
                        Console.WriteLine("Thank you for using.");
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