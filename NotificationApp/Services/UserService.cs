using NotificationSystem.Models;
using NotificationSystem.Interfaces;
using NotificationSystem.Services;
using NotificationSystem.Repositories;

namespace NotificationSystem.Services{
    // User Service implementing IUserInteract interface
    internal class UserService:IUserInteract{

        public UserRepository userRepository;

        public UserService(){
            userRepository=new UserRepository();
        }

        public User CreateUser(){
            User user = GetInputUserDetails();
            userRepository.Create(user);
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
            return userRepository.GetByEmail(emailId);
        }

        public User getUserByMobileNumber(string mobileNumber){
            return userRepository.GetByMobileNumber(mobileNumber);
        }

        public User UpdateUser(string mobileNumber){
             User oldUser=getUserByMobileNumber(mobileNumber);
             if(oldUser==null){
                return null;
             }
             User user = GetInputUserDetails();
             user.UserId=oldUser.UserId;
             return userRepository.Update(user.UserId,user);
        }

        public User DeleteUser(string mobileNumber){
            User user = getUserByMobileNumber(mobileNumber);
            if(user==null){
                return null;
            }
            return userRepository.Delete(user.UserId);
        }

        public User GetUserById(int id){
            return userRepository.Get(id);
        }

    }
}