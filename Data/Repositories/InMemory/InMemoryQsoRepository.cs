using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HamBlocks.Library.Models;
using HamBusLog.Data.Repositories.Abstractions;

namespace HamBusLog.Data.Repositories.InMemory;

// In-memory repository for early development; replace with persistent store implementation.
public sealed class InMemoryQsoRepository : IQsoRepository, IUnitOfWork
{
    private readonly List<Qso> _items = new();

    public Task<IReadOnlyList<Qso>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Qso>>(_items.ToList());

    public Task AddAsync(Qso qso, CancellationToken cancellationToken = default)
    {
        _items.Add(qso);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}


