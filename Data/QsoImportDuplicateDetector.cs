namespace HamBusLog.Data;

using HamBusLog.Wa1gonLib.Models;

public static class QsoImportDuplicateDetector
{
    public static async Task<bool> IsDuplicateAsync(
        HamBusLogDbContext db,
        Qso qso,
        CancellationToken cancellationToken = default)
    {
        if (qso is null)
            return false;

        if (qso.Id != Guid.Empty)
        {
            var idExists = await db.Qsos
                .AsNoTracking()
                .AnyAsync(x => x.Id == qso.Id, cancellationToken);
            if (idExists)
                return true;
        }

        var signature = CreateSignature(new DuplicateProbe(
            qso.Call,
            qso.StationCallSign,
            qso.QsoDate,
            qso.Band,
            qso.Mode,
            qso.Freq));

        var existingKeys = await db.Qsos
            .AsNoTracking()
            .Where(x => x.QsoDate == qso.QsoDate)
            .Select(x => new DuplicateProbe(x.Call, x.StationCallSign, x.QsoDate, x.Band, x.Mode, x.Freq))
            .ToListAsync(cancellationToken);

        return existingKeys
            .Select(CreateSignature)
            .Any(x => string.Equals(x, signature, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<QsoImportDuplicateFilterResult> FilterNewQsosAsync(
        HamBusLogDbContext db,
        IReadOnlyCollection<Qso> imported,
        CancellationToken cancellationToken = default)
    {
        if (imported.Count == 0)
            return new QsoImportDuplicateFilterResult([], 0, []);

        var existingIds = await db.Qsos
            .AsNoTracking()
            .Where(x => x.Id != Guid.Empty)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingRows = await db.Qsos
            .AsNoTracking()
            .Select(x => new { x.Id, Probe = new DuplicateProbe(x.Call, x.StationCallSign, x.QsoDate, x.Band, x.Mode, x.Freq) })
            .ToListAsync(cancellationToken);

        var knownIds = existingIds.ToHashSet();
        var idBySignature = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in existingRows)
            idBySignature[CreateSignature(row.Probe)] = row.Id;

        var accepted = new List<Qso>(imported.Count);
        var duplicateMerges = new List<QsoImportDuplicateMerge>();
        var duplicateCount = 0;

        foreach (var qso in imported)
        {
            var signature = CreateSignature(new DuplicateProbe(qso.Call, qso.StationCallSign, qso.QsoDate, qso.Band, qso.Mode, qso.Freq));
            var existingIdById = qso.Id != Guid.Empty && knownIds.Contains(qso.Id) ? qso.Id : (Guid?)null;
            var existingIdBySignature = idBySignature.TryGetValue(signature, out var matchedId) ? matchedId : (Guid?)null;
            var existingId = existingIdById ?? existingIdBySignature;

            if (existingId is { } duplicateId)
            {
                duplicateCount++;
                duplicateMerges.Add(new QsoImportDuplicateMerge(duplicateId, qso));
                continue;
            }

            accepted.Add(qso);
            if (qso.Id != Guid.Empty)
                knownIds.Add(qso.Id);
            idBySignature[signature] = qso.Id;
        }

        return new QsoImportDuplicateFilterResult(accepted, duplicateCount, duplicateMerges);
    }

    private static string CreateSignature(DuplicateProbe qso)
    {
        var call = NormalizeText(qso.Call);
        var stationCallSign = NormalizeText(qso.StationCallSign);
        var date = $"{qso.QsoDate.Year:0000}{qso.QsoDate.Month:00}{qso.QsoDate.Day:00}{qso.QsoDate.Hour:00}{qso.QsoDate.Minute:00}{qso.QsoDate.Second:00}";
        var band = NormalizeText(qso.Band);
        var mode = NormalizeText(qso.Mode);
        var freq = decimal.Round(qso.Freq, 6, MidpointRounding.AwayFromZero).ToString("0.000000", CultureInfo.InvariantCulture);
        return string.Join("|", call, stationCallSign, date, band, mode, freq);
    }

    private static string NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();

    private readonly record struct DuplicateProbe(string Call, string StationCallSign, DateTime QsoDate, string Band, string Mode, decimal Freq);
}

public readonly record struct QsoImportDuplicateFilterResult(
    IReadOnlyList<Qso> Accepted,
    int DuplicateCount,
    IReadOnlyList<QsoImportDuplicateMerge> DuplicateMerges);

public readonly record struct QsoImportDuplicateMerge(Guid ExistingQsoId, Qso IncomingQso);
