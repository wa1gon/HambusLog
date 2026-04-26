namespace HamBusLog.Data.Repositories.Sqlite;

/// <summary>
/// SQLite-backed repository for QSO records.
/// </summary>
public sealed class SqliteQsoRepository : IQsoRepository, IUnitOfWork
{
    private readonly HamBusLogDbContext _context;

    public SqliteQsoRepository(HamBusLogDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Qso>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connection
            _ = await _context.Database.CanConnectAsync(cancellationToken);

            var qsos = await _context.Qsos
                .ToListAsync(cancellationToken);

            return qsos;
        }
        catch
        {
            throw;
        }
    }

    public async Task<Qso?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Qsos
            .Include(q => q.Details)
            .Include(q => q.QslInfo)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task AddAsync(Qso qso, CancellationToken cancellationToken = default)
    {
        await _context.Qsos.AddAsync(qso, cancellationToken);
    }

    public async Task UpdateAsync(Qso qso, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Qsos
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == qso.Id, cancellationToken);

        if (existing is null)
            return;

        existing.Call = qso.Call;
        existing.QsoDate = qso.QsoDate;
        existing.Freq = qso.Freq;
        existing.Mode = qso.Mode;
        existing.RstRcvd = qso.RstRcvd;
        existing.RstSent = qso.RstSent;
        existing.Band = qso.Band;
        existing.LastUpdate = DateTime.UtcNow;

        existing.Details ??= [];
        existing.Details.Clear();

        if (qso.Details is { Count: > 0 })
        {
            foreach (var detail in qso.Details)
            {
                existing.Details.Add(new QsoDetail
                {
                    QsoId = existing.Id,
                    FieldName = detail.FieldName,
                    FieldValue = detail.FieldValue
                });
            }
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}





