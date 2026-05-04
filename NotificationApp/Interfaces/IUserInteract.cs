using NotificationSystem.Models;

namespace NotificationSystem.Interfaces{
    internal interface IUserInteract{
        public User CreateUser();
        public User getUserByEmail(string emailId);
        public User getUserByMobileNumber(string mobileNumber);
    }
}