
namespace NotificationApp.DALLibrary.Interfaces
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