namespace UnderstandingOOPSApp.Models
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