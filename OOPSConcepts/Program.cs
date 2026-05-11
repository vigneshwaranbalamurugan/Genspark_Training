// using UnderstandingOOPSApp.Interfaces;
// using UnderstandingOOPSApp.Services;
// using UnderstandingOOPSApp.Repositories;
// using UnderstandingOOPSApp.Models;

// namespace UnderstandingOOPSApp
// {
//     internal class Program
//     {
//         ICustomerInteract customerInteract;
//         public Program()
//         {
//             customerInteract = new CustomerService();
//         }
//         void DoBanking()
//         {
//             int choice=0;
//             while(choice!=4){
//                 Console.WriteLine("Welcome to the Banking\nPlease Select the option:\n1. Create Account.\n2. Print Account details using account number\n3. Print Account details using phone number\n4. Exit");

//                 choice =Convert.ToInt32(Console.ReadLine());
//                 switch(choice){
//                     case 1:
//                         var account = customerInteract.OpensAccount();
//                         Console.WriteLine(account);
//                         break;
//                     case 2:
//                         Console.WriteLine("Please enter the account number you like to see:");
//                         string accNum=Console.ReadLine()??"";
//                         customerInteract.PrintAccountDetailsByAccNumber(accNum);
//                         break;
//                     case 3:
//                         Console.WriteLine("Please enter the mobile number you like to see:");
//                         string phoneNum=Console.ReadLine()??"";
//                         customerInteract.PrintAccountDetailsByPhone(phoneNum);
//                         break;
//                     case 4:
//                         Console.WriteLine("Thank you for using our service");
//                         break;
//                     default:
//                         Console.WriteLine("Please enter the correct choice");
//                         break;
//                 }
//             }
//         }
//         static void Main(string[] args)
//         {
//             // new Program().DoBanking();

//             // AccountRepository accountRepository = new AccountRepository();
//             // var account = new Account("", "Vigneshwaran", new DateTime(), "vigneshwaran@gmail.com", "9876543210", 10000);
//             // account = accountRepository.Create(account);
//             // Console.WriteLine(account);
//             // accountRepository[account.AccountNumber] = new Account();
//             // Console.WriteLine(accountRepository[account.AccountNumber]);

//             //Operator Overloading example
//             Account account1 = new Account("9990001001", "Vigneshwaran", new DateTime(), "vignesh@gmail.com", "9876543210", 10000);
//             Account account2 = new Account("9990001001", "Vigneshwaran", new DateTime(), "vigi@gmail.com", "9876543210", 20000);
//             if(account1.Equals(account2))
//                 Console.WriteLine("Both accounts are same");
//             else
//                 Console.WriteLine("Both accounts are different");
//         }
//     }
// }


using UnderstandingOOPSApp.Models;
namespace BankingFEApplication
{
    internal class Program
    {
        // public delegate void MyDelegate(int n1, int n2);//Declare the type

        public delegate void MyDelegate<T,K>(T n1, K n2);
        
        Action<int,int> delegateRef;//refference for the type
        //MyDelegate<int, int> del;

        public void Add(int num1, int num2)//Method that could be delegated
        {
            var result = num1 + num2;
            Console.WriteLine($"The sum of {num1} and {num2} is {result}");
        }

        public void Product(int num1, int num2)//Method that could be delegated
        {
            var result = num1 * num2;
            Console.WriteLine($"The product of {num1} and {num2} is {result}");
        }

        public Program()//Constructore for instan
        {
            delegateRef = new Action<int,int>(Product);
            //delegateRef += delegate (int num1, int num2) //anon method
            //{
            //    var result = num1 + num2;
            //    Console.WriteLine($"The sum of {num1} and {num2} is {result}");
            //};

            delegateRef += (num1, num2)=> Console.WriteLine($"The sum of {num1} and {num2} is {(num1+num2)}");

            delegateRef -= Product;
        }

        void Calculate(Action<int,int> del) //takes functionality as parameter
        {
            del(100, 200);
        }
        static void Main(string[] args)
        {
            //Program program = new Program();
            //program.Calculate(program.delegateRef);

            List<Account> accounts = new List<Account>()
            {
                new SavingAccount{AccountNumber="12345",NameOnAccount="Ramu",Balance=12342.2f,DateOfBirth=new DateTime(2000,12,12),Email="Ramu@gmail.com",Phone="9876543210"},
                new CurrentAccount{AccountNumber="12346",NameOnAccount="Bamu",Balance=54321.5f,DateOfBirth=new DateTime(1999,12,12),Email="Bamu@gmail.com",Phone="4321098765"}
            };

            var givenAccountNumber = "12346";
            Predicate<Account> checkAccountWithAccNum = (account) =>account.AccountNumber == givenAccountNumber;
            Console.WriteLine(checkAccountWithAccNum(
                new CurrentAccount() { AccountNumber = "12345", NameOnAccount = "Bamu", Balance = 54321.5f, DateOfBirth = new DateTime(1999, 12, 12), Email = "Bamu@gmail.com", Phone = "4321098765" }));
        }
    }
}



// namespace BankingFEApplication
// {
//     internal class Program
//     {
//         public delegate void MyDelegate(int n1, int n2);//Declare the type

//         MyDelegate delegateRef;//refference for the type

//         public void Add(int num1, int num2)//Method that could be delegated
//         {
//             var result = num1 + num2;
//             Console.WriteLine($"The sum of {num1} and {num2} is {result}");
//         }

//         public void Product(int num1, int num2)//Method that could be delegated
//         {
//             var result = num1 * num2;
//             Console.WriteLine($"The product of {num1} and {num2} is {result}");
//         }

//         public Program()//Constructore for instan
//         {
//             delegateRef = new MyDelegate(Product);
//         }

//         void Calculate(MyDelegate del) //takes functionality as parameter
//         {
//             del(100, 200);
//         }
//         static void Main(string[] args)
//         {
//             Program program = new Program();
//             program.Calculate(program.delegateRef);
//         }
//     }
// }
