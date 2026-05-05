using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotificationSystem.Models;

namespace NotificationSystem.Interfaces
{
    // Generic Repository Interface
    internal interface IRepository<K,T> where T : class
    {
        public T Create(T item);
        public T? Get(K key);
        public List<T>? GetAll();

        public T? Update(K key,T item);
        public T? Delete(K key);

    }
}