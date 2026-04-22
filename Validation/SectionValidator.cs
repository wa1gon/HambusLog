namespace HamBusLog.Validation;

public sealed class SectionValidator
{
    public ValidationResult Validate(string? value)
    {
        // TODO: Add ARRL section validation rules.
        return ValidationResult.Success();
    }
}

