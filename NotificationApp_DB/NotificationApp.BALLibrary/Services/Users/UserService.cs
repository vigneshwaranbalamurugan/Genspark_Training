using NotificationApp.ModelLibrary.Models;
using NotificationApp.BALLibrary.Interfaces;
using NotificationApp.DALLibrary.Repositories;

namespace NotificationApp.BALLibrary.Services{
    // User Service implementing IUserInteract interface
    public class UserService:IUserInteract{

        public UserRepository userRepository;

        public UserService(UserRepository userRepository){
            this.userRepository = userRepository;
        }

        public User CreateUser(string userName,string emailId,string mobileNumber){
            User user = new User{
                UserName = userName,
                EmailId = emailId,
                MobileNumber = mobileNumber
            };
            userRepository.Create(user);
            return user;
        }

        public User getUserByEmail(string emailId){
            return userRepository.GetByEmail(emailId);
        }

        public User getUserByMobileNumber(string mobileNumber){
            return userRepository.GetByMobileNumber(mobileNumber);
        }

        public User UpdateUser(string mobileNumber,string newUserName,string newEmailId,string newMobileNumber){
             User oldUser=getUserByMobileNumber(mobileNumber);
             if(oldUser==null){
                return null;
             }
            User user = new User
            {
                UserName = newUserName,
                EmailId = newEmailId,
                MobileNumber = newMobileNumber,
                UserId = oldUser.UserId
            };
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