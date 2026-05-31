namespace HamBusLog.Services;

using HamBusLog.Models;

public interface ICallsignLookupProvider
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    Task<CallsignLookupResult?> LookupAsync(string callSign, CancellationToken cancellationToken);
}

