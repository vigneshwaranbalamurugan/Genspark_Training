using NotificationApp.ModelLibrary.Models;

namespace NotificationApp.DALLibrary.Repositories
{
    public class NotificationRepository : AbstractRepository<int, Notification>
    {
        public NotificationRepository()
        {
            _items=new Dictionary<int, Notification>();
        }

        public Notification this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }

        static int lastId = 0;

        public override Notification Create(Notification notification)
        {
            ++lastId;
            notification.Id = lastId;
            _items.Add(notification.Id, notification);
            return notification;
        }

        public Notification? GetById(int id)
        {
            if (_items.ContainsKey(id))
            {
                return _items[id];
            }
            return null;
        }
       
    }
}