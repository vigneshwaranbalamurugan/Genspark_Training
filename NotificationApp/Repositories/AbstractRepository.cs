using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotificationSystem.Interfaces;
using NotificationSystem.Models;

namespace NotificationSystem.Repositories{
    // Generic Abstract Repository implementing IRepository interface
    internal abstract class AbstractRepository<K,T>:IRepository<K,T> where T:class{
        protected Dictionary<K,T> _items;

        public abstract T Create(T item);

        public T? Get(K key){
            if(_items.ContainsKey(key)){
                return _items[key];
            }
            return null;
        }

        public List<T>? GetAll(){
            if(_items.Count==0){
                return null;
            }
            return _items.Values.ToList();
        }

        public T? Update(K key,T item){
            if(_items.ContainsKey(key)){
                _items[key]=item;
                return item;
            }
            return null;
        }

        public T? Delete(K key){
            if(_items.ContainsKey(key)){
                T item=_items[key];
                _items.Remove(key);
                return item;
            }
            return null;
        }

    }
}