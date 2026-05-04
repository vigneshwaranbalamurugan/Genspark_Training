
namespace UnderstandingOOPSApp.Models
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