using NotificationSystem.Models;

namespace NotificationSystem.Interfaces{
    // User Interaction Interface
    internal interface IUserInteract{
        public User CreateUser();
        public User getUserByEmail(string emailId);
        public User getUserByMobileNumber(string mobileNumber);
        public User UpdateUser(string mobileNumber);
        public User DeleteUser(string mobileNumber);
        public User GetUserById(int id);
    }
}