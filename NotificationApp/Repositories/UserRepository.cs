using System;
using System.Collections.Generic;
using NotificationSystem.Interfaces;
using NotificationSystem.Models;

namespace NotificationSystem.Repositories{

    // User Repository class inheriting AbstractRepository
    internal class UserRepository:AbstractRepository<int,User>{
        
        public UserRepository(){
            _items=new Dictionary<int,User>();        
        }

        public User this[int index]{
            get{return _items[index];}
            set{_items[index]=value;}
        }
        static int lastId=0;

        public override User Create(User user){
            ++lastId;
            user.UserId=lastId;
            _items.Add(user.UserId,user);
            return user;
        }

        public User? GetByEmail(string emailId){
            foreach(User user in _items.Values){
                if(user.EmailId==emailId){
                    return user;
                }
            }
            return null;
        }

        public User? GetByMobileNumber(string mobileNumber){
            foreach(User user in _items.Values){
                if(user.MobileNumber==mobileNumber){
                    return user;
                }
            }
            return null;
        }
    }
}