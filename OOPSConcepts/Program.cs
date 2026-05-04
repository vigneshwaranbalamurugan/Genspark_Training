using UnderstandingOOPSApp.Interfaces;
using UnderstandingOOPSApp.Services;

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
            new Program().DoBanking();
        }
    }
}