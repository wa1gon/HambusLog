namespace HamBusLog.Wa1gonLib.Exchange;

public class JadeValidationException : Exception
{
    public JadeValidationException(string message, IReadOnlyList<string> errors)
        : base(message)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}

