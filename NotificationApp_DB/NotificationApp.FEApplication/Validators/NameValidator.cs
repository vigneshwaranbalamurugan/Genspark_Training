using NotificationApp.ModelLibrary.Exceptions;

namespace NotificationApp.FEApplication.Validators
{
    public class NameValidator
    {
        public string GetUserInputForName(string promptMessage = "Enter Name:")
        {
            string name;
            while (true)
            {
                try
                {
                    Console.WriteLine(promptMessage);
                    name = Console.ReadLine() ?? "";
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new InvalidNameException($"Invalid Name:{name}, Name cannot be empty.");
                    }
                    if (!name.All(c => char.IsLetter(c) || c == ' '))
                    {
                        throw new InvalidNameException($"Invalid Name:{name}, Name must contain only alphabets");
                    }
                    if (name.Length < 3)
                    {
                        throw new InvalidNameException($"Invalid Name:{name}, Name must be at least 3 characters long.");
                    }
                    return name;
                }
                catch (InvalidNameException ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
        }
    }
}