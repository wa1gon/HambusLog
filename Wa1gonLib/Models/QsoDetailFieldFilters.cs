namespace HamBusLog.Wa1gonLib.Models;

public static class QsoDetailFieldFilters
{
    private static readonly HashSet<string> ExcludedRigMetadataFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "RADIO_NAME",
        "RADIO_LABEL",
        "RADIO_MODE"
    };

    public static bool IsExcluded(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return false;

        return ExcludedRigMetadataFields.Contains(fieldName.Trim());
    }

    public static IReadOnlyCollection<string> RigMetadataFields => ExcludedRigMetadataFields;
}

