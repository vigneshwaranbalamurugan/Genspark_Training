using Microsoft.EntityFrameworkCore;
using UnderstandingEfCoreApp.Models;
using UnderstandingEfCoreApp.Contexts;

using (var context = new BankingContext())
{
    var customers = context.customers.Include(c => c.Accounts).ToList();

    foreach (var customer in customers)
    {
        Console.WriteLine($"Customer: {customer.Name}, Email: {customer.Email}");
        if (customer.Accounts != null)
        {
            foreach (var account in customer.Accounts)
            {
                Console.WriteLine($"\tAccount Number: {account.AccountNumber}, Balance: {account.Balance}");
            }
        }
    }
}



