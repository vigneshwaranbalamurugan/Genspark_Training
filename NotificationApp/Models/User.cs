using System;

namespace NotificationSystem.Models{

    // User Class
    internal class User{
        public int UserId{get;set;}
        public string UserName{get;set;}
        public string MobileNumber{get;set;}
        public string EmailId{get;set;}

        public User(){
            UserName=string.Empty;
            MobileNumber=string.Empty;
            EmailId=string.Empty;
        }

        public User(int userId,string userName,string mobNum,string emailId){
            UserId=userId;
            UserName=userName;
            MobileNumber=mobNum;
            EmailId=emailId;
        }

        public override string ToString(){
            return $"User Id: {UserId}\nUser Name: {UserName}\nMobile Number: {MobileNumber}\nEmail Id: {EmailId}";
        }
    }
}