namespace NotificationApp.ModelLibrary.Exceptions{
public class InvalidMobileNumberException : Exception
{
    string _message;
    public InvalidMobileNumberException()
    {
        _message = "Invalid Mobile Number. Mobile number should be 10 digits long and contain only numbers.";
    }

    public InvalidMobileNumberException(string message)
    {
        _message = message;
    }
    
    public override string Message => _message;
}

}