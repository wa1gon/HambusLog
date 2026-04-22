using System.Threading;
using System.Threading.Tasks;

namespace HamBusLog.Hardware.Interfaces;

public interface IRadioStateProvider
{
    Task<string?> GetBandAsync(CancellationToken cancellationToken = default);

    Task<decimal?> GetFrequencyAsync(CancellationToken cancellationToken = default);

    Task<string?> GetModeAsync(CancellationToken cancellationToken = default);
}

