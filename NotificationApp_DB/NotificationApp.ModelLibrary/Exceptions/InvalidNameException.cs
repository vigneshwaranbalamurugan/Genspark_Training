namespace NotificationApp.ModelLibrary.Exceptions{
public class InvalidNameException : Exception
{
    string _message;    
    public InvalidNameException()
    {
        _message = "Invalid Name. Name cannot be empty and should contain only alphabets and at least 3 characters.";
    }

    public InvalidNameException(string message)
    {
        _message = message;
    }
    public override string Message => _message;
}
}