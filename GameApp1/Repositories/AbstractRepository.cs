using GameApp.Interfaces;

namespace GameApp.Repositories{
    internal abstract class AbstractRepository<T>:IRepository<T>{
        protected readonly List<T> _items = new List<T>();

        public void Add(T item){
            _items.Add(item);
        }

        public T GetLast(){
            return _items[_items.Count - 1];
        }

        public T this[int index]{
            get { return _items[index]; }
        }

        public List<T> GetAll(){
            return _items;
        }

        public void Clear(){
            _items.Clear();
        }
    }
}