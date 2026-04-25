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
        System.Diagnostics.Debug.WriteLine("SqliteQsoRepository created with DbContext");
    }

    public async Task<IReadOnlyList<Qso>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("SqliteQsoRepository.GetAllAsync() called");
            
            // Test connection
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine($"Database connection successful: {canConnect}");
            
            // First, try without includes to see if basic query works
            var qsos = await _context.Qsos
                .ToListAsync(cancellationToken);
            
            System.Diagnostics.Debug.WriteLine($"SqliteQsoRepository: Retrieved {qsos.Count} QSOs from database");
            
            // Log details of each QSO
            foreach (var qso in qsos)
            {
                System.Diagnostics.Debug.WriteLine($"  QSO: Call={qso.Call}, Date={qso.QsoDate}, Freq={qso.Freq}, Mode={qso.Mode}");
            }
            
            return qsos;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetAllAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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





