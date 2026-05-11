namespace NotificationApp.ModelLibrary.Exceptions{
public class MessageException : Exception
{
    string _message;
    public MessageException()
    {
        _message = "Message cannot be empty and should be greater than 5 and less than 160 characters.";
    }

    public MessageException(string message)
    {
        _message = message;
    }
    
    public override string Message => _message;
}
}