using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnderstandingOOPSApp.Models;

namespace UnderstandingOOPSApp.Interfaces
{
    internal interface ICustomerInteract
    {
        public Account OpensAccount();
        public void PrintAccountDetailsByAccNumber(string accountNumber);
        public void PrintAccountDetailsByPhone(string phoneNumber);
    }
}