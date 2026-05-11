using NotificationApp.ModelLibrary.Exceptions;
using System.Text.RegularExpressions;

namespace NotificationApp.FEApplication.Validators
{
    public class EmailValidator
    {
        public string GetAndValidateEmail(string promptMessage = "Enter Email ID:")
        {
            string emailId;
            while (true)
            {
                try
                {
                    Console.WriteLine(promptMessage);
                    emailId = Console.ReadLine() ?? "";
                    if (string.IsNullOrEmpty(emailId))
                    {
                        throw new InvalidEmailIdException($"Invalid Email ID:{emailId}, Email ID cannot be empty.");
                    }
                    string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                    if (!Regex.IsMatch(emailId, pattern, RegexOptions.IgnoreCase))
                    {
                        throw new InvalidEmailIdException($"Invalid Email ID:{emailId}, Email ID is not in a valid format.");
                    }
                    return emailId;
                }
                catch (InvalidEmailIdException ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
        }
    }
}
