using System.Collections.Generic;
using UnderstandingOOPSApp.Models;

namespace UnderstandingOOPSApp.Interfaces
{
    internal interface IRepository<TKey, TValue>
    {
        TValue Create(TValue item);
        TValue? Delete(TKey key);
        TValue? GetAccount(TKey key);
        List<TValue>? GetAccounts();
        TValue? Update(TKey key, TValue item);
    }
}