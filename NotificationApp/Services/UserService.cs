using NotificationSystem.Models;
using NotificationSystem.Interfaces;

namespace NotificationSystem.Services{
    internal class UserService:IUserInteract{
        List<User> users = new List<User>();
        static int lastUserId=100;

        public User CreateUser(){
            User user = GetInputUserDetails();
            user.UserId=(++lastUserId);
            users.Add(user);
            return user;
        }

        private User GetInputUserDetails(){
            User user=new User();
            Console.WriteLine("Enter the User Name:");
            user.UserName=Console.ReadLine()??"";
            Console.WriteLine("Enter the Email id:");
            user.EmailId=Console.ReadLine()??"";
            Console.WriteLine("Enter the Mobile Number:");
            user.MobileNumber=Console.ReadLine()??"";
            return user;
        }

        public User getUserByEmail(string emailId){
            User user = users.FirstOrDefault(u=>u.EmailId==emailId);
            if(user!=null){
                return user;
            }
            return null;
        }

        public User getUserByMobileNumber(string mobileNumber){
            User user = users.FirstOrDefault(u=>u.MobileNumber==mobileNumber);
            if(user!=null){
                return user;
            }
            return null;
        }
    }
}