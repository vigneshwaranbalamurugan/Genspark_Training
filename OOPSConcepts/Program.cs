using UnderstandingOOPSApp.Interfaces;
using UnderstandingOOPSApp.Services;
using UnderstandingOOPSApp.Repositories;
using UnderstandingOOPSApp.Models;

namespace UnderstandingOOPSApp
{
    internal class Program
    {
        ICustomerInteract customerInteract;
        public Program()
        {
            customerInteract = new CustomerService();
        }
        void DoBanking()
        {
            int choice=0;
            while(choice!=4){
                Console.WriteLine("Welcome to the Banking\nPlease Select the option:\n1. Create Account.\n2. Print Account details using account number\n3. Print Account details using phone number\n4. Exit");

                choice =Convert.ToInt32(Console.ReadLine());
                switch(choice){
                    case 1:
                        var account = customerInteract.OpensAccount();
                        Console.WriteLine(account);
                        break;
                    case 2:
                        Console.WriteLine("Please enter the account number you like to see:");
                        string accNum=Console.ReadLine()??"";
                        customerInteract.PrintAccountDetailsByAccNumber(accNum);
                        break;
                    case 3:
                        Console.WriteLine("Please enter the mobile number you like to see:");
                        string phoneNum=Console.ReadLine()??"";
                        customerInteract.PrintAccountDetailsByPhone(phoneNum);
                        break;
                    case 4:
                        Console.WriteLine("Thank you for using our service");
                        break;
                    default:
                        Console.WriteLine("Please enter the correct choice");
                        break;
                }
            }
        }
        static void Main(string[] args)
        {
            // new Program().DoBanking();

            // AccountRepository accountRepository = new AccountRepository();
            // var account = new Account("", "Vigneshwaran", new DateTime(), "vigneshwaran@gmail.com", "9876543210", 10000);
            // account = accountRepository.Create(account);
            // Console.WriteLine(account);
            // accountRepository[account.AccountNumber] = new Account();
            // Console.WriteLine(accountRepository[account.AccountNumber]);

            //Operator Overloading example
            Account account1 = new Account("9990001001", "Vigneshwaran", new DateTime(), "vignesh@gmail.com", "9876543210", 10000);
            Account account2 = new Account("9990001001", "Vigneshwaran", new DateTime(), "vigi@gmail.com", "9876543210", 20000);
            if(account1.Equals(account2))
                Console.WriteLine("Both accounts are same");
            else
                Console.WriteLine("Both accounts are different");
        }
    }
}