namespace HamBusLog.Validation;

public sealed class BandValidator
{
    public ValidationResult Validate(string? value)
    {
        // TODO: Add explicit contest/band rule checks.
        return ValidationResult.Success();
    }
}

