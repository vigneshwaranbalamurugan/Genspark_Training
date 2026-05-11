using System;
using NotificationApp.BALLibrary.Interfaces;
using NotificationApp.BALLibrary.Services;
using NotificationApp.DALLibrary.Repositories;


namespace NotificationApp.FEApplication{
    internal class Program{
        private UserApp userApp;
        private NotificationApp notificationApp;
        UserRepository userRepository;
        NotificationRepository notificationRepository;
        IUserInteract userInteract;

        INotificationService notificationService;
        public Program(){
            userRepository = new UserRepository();
            notificationRepository = new NotificationRepository();
            userInteract = new UserService(userRepository);
            userApp = new UserApp(userInteract);
            notificationService = new NotificationService(userInteract, notificationRepository);
            notificationApp = new NotificationApp(notificationService);
        }

        // Main method to run the application
        public void Run(){
            int choice=0;
            Console.WriteLine("--Welcome to Notification Application--");
            while(choice!=3){
               Console.WriteLine("Select the option:\n1. User Menu\n2. Notification Menu\n3. Exit");
               while(true){
               try{
                   choice = int.Parse(Console.ReadLine() ?? "0");
                   break;
                }catch(FormatException){
                    Console.WriteLine("Invalid input. Please enter a number.");
                    Console.WriteLine("\n1. User Menu\n2. Notification Menu\n3. Exit");
                    continue;
                }catch(Exception ex){
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.WriteLine("\n1. User Menu\n2. Notification Menu\n3. Exit");
                    continue;
                }
               }
               switch(choice){
                    case 1:
                        UserMenu();
                        break;
                    case 2:
                        NotificationMenu();
                        break;
                    case 3:
                        Console.WriteLine("Thank you for using notification application.");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
               }
            }
        }

        private void UserMenu(){
            int choice = 0;
            while(choice!=5){
                Console.WriteLine("Select User action:\n1. Get User Details\n2. Create User\n3. Update User Details\n4. Delete User\n5. Back");
                
                while(true){
                try{
                    choice = int.Parse(Console.ReadLine() ?? "0");
                    break;
                }catch(FormatException){
                    Console.WriteLine("Invalid input. Please enter a number.");
                    Console.WriteLine("\n1. User Menu\n2. Notification Menu\n3. Exit");
                    continue;
                }catch(Exception ex){
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.WriteLine("\n1. User Menu\n2. Notification Menu\n3. Exit");
                    continue;
                }
                }
                switch(choice){
                    case 1:
                        userApp.GetUserDetails();
                        break;
                    case 2:
                        userApp.CreateUser();
                        break;
                    case 3:
                        userApp.UpdateUserDetails();
                        break;
                    case 4:
                        userApp.DeleteUser();
                        break;
                    case 5:
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private void NotificationMenu(){
            int choice = 0;
            while(choice!=3){
                Console.WriteLine("Select Notification action:\n1. Send Notification \n2. Get All Notifications\n3. Back");
                
                while(true){
                    try{
                        choice = int.Parse(Console.ReadLine() ?? "0");
                        break;
                    }catch(FormatException){
                        Console.WriteLine("Invalid input. Please enter a number.");
                        Console.WriteLine("\n1. Send Notification \n2. Get All Notifications\n3. Back");
                        continue;
                    }catch(Exception ex){
                        Console.WriteLine($"An error occurred: {ex.Message}");
                        Console.WriteLine("\n1. Send Notification \n2. Get All Notifications\n3. Back");
                        continue;
                    }
                }
                switch(choice){
                    case 1:
                        notificationApp.SendNotification();
                        break;
                    case 2:
                        notificationApp.GetAllNotifications();
                        break;
                    case 3:
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