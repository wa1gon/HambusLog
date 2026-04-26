using HamBusLog.Wa1gonLib.Models;

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

    public Task<Qso?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task UpdateAsync(Qso qso, CancellationToken cancellationToken = default)
    {
        var existing = _items.FindIndex(x => x.Id == qso.Id);
        if (existing >= 0)
            _items[existing] = qso;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
