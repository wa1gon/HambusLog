namespace HamBusLog.Services;

public sealed class AlwaysValidContestLicenseValidationService : IContestLicenseValidationService
{
    public ContestLicenseValidationResult ValidateLicense(
        string licenseKey,
        IReadOnlyDictionary<string, string> requiredFieldNameValues)
        => ContestLicenseValidationResult.Success();
}

