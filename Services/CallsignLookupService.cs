namespace HamBusLog.Services;

using HamBusLog.Models;

public sealed class CallsignLookupService
{
    private readonly IReadOnlyList<ICallsignLookupProvider> _providers;

    public CallsignLookupService(IEnumerable<ICallsignLookupProvider> providers)
    {
        _providers = providers?.Where(p => p is not null).ToList() ?? [];
    }

    public static CallsignLookupService CreateDefault(AppConfiguration config)
    {
        var lookupConfig = config.CallsignLookup ?? new CallsignLookupConfiguration();
        return new CallsignLookupService(new ICallsignLookupProvider[]
        {
            new QrzLookupProvider(lookupConfig.Qrz)
        });
    }

    public async Task<(CallsignLookupResult? Result, string ErrorMessage)> LookupAsync(string? callSign, CancellationToken cancellationToken)
    {
        var normalized = (callSign ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return (null, "Enter a callsign first.");

        foreach (var provider in _providers)
        {
            if (!provider.IsConfigured)
                continue;

            try
            {
                var result = await provider.LookupAsync(normalized, cancellationToken);
                if (result is null)
                    return (null, $"{provider.ProviderName} returned no data.");

                return (result, string.Empty);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        return (null, "No lookup provider is configured.");
    }
}
