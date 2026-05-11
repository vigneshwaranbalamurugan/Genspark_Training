using NotificationApp.ModelLibrary.Exceptions;

namespace NotificationApp.FEApplication.Validators
{
    public class MessageValidator
    {
        public string GetAndValidateMessage(string prompt="Enter Message:")
        {
            while (true)
            {
                Console.WriteLine(prompt);
                string message = Console.ReadLine() ?? "";
                try
                {
                    ValidateMessage(message);
                    return message;
                }
                catch (MessageException ex)
                {
                    Console.WriteLine($"Invalid input: {ex.Message}");
                    continue;
                }
            }
        }
        private void ValidateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new MessageException($"Invalid Message: {message}, Message cannot be empty.");
            }
            if (message.Length < 5 || message.Length > 160)
            {
                throw new MessageException($"Invalid Message: {message}, Message should be greater than 5 and less than 160 characters.");
            }
        }
    }
}