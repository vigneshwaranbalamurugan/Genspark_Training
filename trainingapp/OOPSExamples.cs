using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderstandingOOPSApp
{
    internal class Account
    {
        /*private string accountNumber;

        //Customized get and set 
        public string AccountNumber1 { get
            {
                var accountNum = accountNumber.Substring(7, 4);
                return "********"+accountNum;
            }
            set
            {
                accountNumber = value;
            }
        }*/
        /*
        Getter and Setter Function
        public void SetAccountNumber(string accountNumber)
        { 
           this.AccountNumber = accountNumber; 
        }
        public string GetAccountNumber()
        {
           return this.AccountNumber ;
        }
        */

        //public int AccountNumber { get; set; }

        public enum AccType
        {
            SavingAccount =1,CurrentAccount=2
        }

        public  string AccountNumber { get; set; } =string.Empty;
        public string NameOnAccount { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public float Balance { get; set; }
        public AccType AccountType { get; set; }

        public Account(){}

        public Account(string accountNumber, string nameOnAccount, DateTime dateOfBirth, string email, string phone, float balance)
        {
            AccountNumber = accountNumber;
            NameOnAccount = nameOnAccount;
            DateOfBirth = dateOfBirth;
            Email = email;
            Phone = phone;
            Balance = balance;
        }

        public override string ToString()
        {
            return $"Account Number : {AccountNumber}\nAccount Holder Name : {NameOnAccount}\nPhone Number : {Phone}\n" +
                $"Email : {Email}\nBalance : ${Balance}";
        }
    }
}



namespace UnderstandingOOPSApp
{
    internal class CurrentAccount : Account
    {
        public CurrentAccount()
        {
            AccountType = AccType.CurrentAccount;
            Balance = 0.0f;
        }
    }
}

namespace UnderstandingOOPSApp
{
    internal class SavingAccount :Account
    {
        public SavingAccount()
        {
            AccountType = AccType.SavingAccount;
            Balance = 100.0f;
        }
    }
}