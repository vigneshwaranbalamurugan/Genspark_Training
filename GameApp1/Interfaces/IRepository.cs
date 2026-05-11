
using System.Collections.Generic;

namespace GameApp.Interfaces{
    internal interface IRepository<T>{
        void Add(T item);
        T GetLast();
        void Clear();
        List<T> GetAll();
    }
}