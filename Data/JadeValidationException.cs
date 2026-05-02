namespace HamBusLog.Data;

[Obsolete("Use HamBusLog.Wa1gonLib.Exchange.JadeValidationException instead.")]
public sealed class JadeValidationException : HamBusLog.Wa1gonLib.Exchange.JadeValidationException
{
    public JadeValidationException(string message, IReadOnlyList<string> errors)
        : base(message, errors)
    {
    }
}


