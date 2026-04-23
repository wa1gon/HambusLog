namespace HamBusLog.Hardware.Interfaces;

// Default no-op implementation until CAT/rig integrations are wired.
public sealed class NullRadioStateProvider : IRadioStateProvider
{
    public Task<string?> GetBandAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

    public Task<decimal?> GetFrequencyAsync(CancellationToken cancellationToken = default) => Task.FromResult<decimal?>(null);

    public Task<string?> GetModeAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
}
