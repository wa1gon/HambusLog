using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HamBlocks.Library.Models;

namespace HamBusLog.Data.Repositories.Abstractions;

public interface IQsoRepository
{
    Task<IReadOnlyList<Qso>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Qso qso, CancellationToken cancellationToken = default);
}

