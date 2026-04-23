namespace HamBusLog.Validation;

public sealed class ValidationResult
{
    public static ValidationResult Success() => new(true, string.Empty);

    public static ValidationResult Failure(string message) => new(false, message);

    private ValidationResult(bool isValid, string errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public bool IsValid { get; }

    public string ErrorMessage { get; }
}

