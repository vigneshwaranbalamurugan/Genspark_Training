using NotificationApp.ModelLibrary.Exceptions;

namespace NotificationApp.FEApplication.Validators
{
    public class MobileNumberValidator
    {
        public string GetAndValidateMobileNumber(string promptMessage = "Enter Mobile Number:")
        {
            string mobileNumber;
            while (true)
            {
                try
                {
                    Console.WriteLine(promptMessage);
                    mobileNumber = Console.ReadLine() ?? "";
                    if (string.IsNullOrEmpty(mobileNumber))
                    {
                        throw new InvalidMobileNumberException($"Invalid Mobile Number:{mobileNumber}, Mobile number cannot be empty.");
                    }
                    if (!mobileNumber.All(char.IsDigit))
                    {
                        throw new InvalidMobileNumberException($"Invalid Mobile Number:{mobileNumber}, Mobile number must contain only digits.");
                    }
                    if (mobileNumber.Length != 10)
                    {
                        throw new InvalidMobileNumberException($"Invalid Mobile Number:{mobileNumber}, Mobile number must be 10 digits long.");
                    }
                    return mobileNumber;
                }
                catch (InvalidMobileNumberException ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
        }
    }
}
