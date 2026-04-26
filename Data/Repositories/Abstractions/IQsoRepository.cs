using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.Data.Repositories.Abstractions;

public interface IQsoRepository
{
    Task<IReadOnlyList<Qso>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Qso?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Qso qso, CancellationToken cancellationToken = default);

    Task UpdateAsync(Qso qso, CancellationToken cancellationToken = default);
}
