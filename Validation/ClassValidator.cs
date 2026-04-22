namespace HamBusLog.Validation;

public sealed class ClassValidator
{
    public ValidationResult Validate(string? value)
    {
        // TODO: Add ARRL class validation rules.
        return ValidationResult.Success();
    }
}

