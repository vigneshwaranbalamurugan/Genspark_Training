namespace NotificationApp.ModelLibrary.Exceptions{
public class InvalidEmailIdException : Exception
{
    string _message;
    public InvalidEmailIdException()
    {
        _message = "Invalid Email Id. Email Id should be in the format 'example@domain.com'";
    }

    public InvalidEmailIdException(string message)
    {
        _message = message;
    }

    public override string Message => _message;
}
}