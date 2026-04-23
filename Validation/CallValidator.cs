using System.Text.RegularExpressions;

namespace HamBusLog.Validation;

public sealed class CallValidator
{
    private static readonly Regex AllowedPattern = new("^[A-Z0-9/]+$", RegexOptions.Compiled);

    public string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    public ValidationResult Validate(string? value)
    {
        var candidate = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(candidate))
            return ValidationResult.Failure("Callsign is required.");

        if (!AllowedPattern.IsMatch(candidate))
            return ValidationResult.Failure("Callsign must be uppercase and may only contain A-Z, 0-9, or '/'.");

        return ValidationResult.Success();
    }
}

