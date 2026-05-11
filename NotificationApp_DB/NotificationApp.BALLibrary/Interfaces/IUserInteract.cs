using NotificationApp.ModelLibrary.Models;

namespace NotificationApp.BALLibrary.Interfaces{
    // User Interaction Interface
    public interface IUserInteract{
        public User CreateUser(string userName,string emailId,string mobileNumber);
        public User getUserByEmail(string emailId);
        public User getUserByMobileNumber(string mobileNumber);
        public User UpdateUser(string mobileNumber,string newUserName,string newEmailId,string newMobileNumber);
        public User DeleteUser(string mobileNumber);
        public User GetUserById(int id);
    }
}