namespace HamBusLog.Data;

using HamBusLog.Wa1gonLib.Models;

internal static class QsoImportDuplicateDetector
{
    public static async Task<QsoImportDuplicateFilterResult> FilterNewQsosAsync(
        HamBusLogDbContext db,
        IReadOnlyCollection<Qso> imported,
        CancellationToken cancellationToken = default)
    {
        if (imported.Count == 0)
            return new QsoImportDuplicateFilterResult([], 0);

        var existingIds = await db.Qsos
            .AsNoTracking()
            .Where(x => x.Id != Guid.Empty)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingKeys = await db.Qsos
            .AsNoTracking()
            .Select(x => new DuplicateProbe(x.Call, x.MyCall, x.QsoDate, x.Band, x.Mode, x.Freq))
            .ToListAsync(cancellationToken);

        var knownIds = existingIds.ToHashSet();
        var knownKeys = existingKeys
            .Select(CreateSignature)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var accepted = new List<Qso>(imported.Count);
        var duplicateCount = 0;

        foreach (var qso in imported)
        {
            var signature = CreateSignature(new DuplicateProbe(qso.Call, qso.MyCall, qso.QsoDate, qso.Band, qso.Mode, qso.Freq));
            var isDuplicate = (qso.Id != Guid.Empty && knownIds.Contains(qso.Id))
                              || knownKeys.Contains(signature);

            if (isDuplicate)
            {
                duplicateCount++;
                continue;
            }

            accepted.Add(qso);
            if (qso.Id != Guid.Empty)
                knownIds.Add(qso.Id);
            knownKeys.Add(signature);
        }

        return new QsoImportDuplicateFilterResult(accepted, duplicateCount);
    }

    private static string CreateSignature(DuplicateProbe qso)
    {
        var call = NormalizeText(qso.Call);
        var myCall = NormalizeText(qso.MyCall);
        var date = $"{qso.QsoDate.Year:0000}{qso.QsoDate.Month:00}{qso.QsoDate.Day:00}{qso.QsoDate.Hour:00}{qso.QsoDate.Minute:00}{qso.QsoDate.Second:00}";
        var band = NormalizeText(qso.Band);
        var mode = NormalizeText(qso.Mode);
        var freq = decimal.Round(qso.Freq, 6, MidpointRounding.AwayFromZero).ToString("0.000000", CultureInfo.InvariantCulture);
        return string.Join("|", call, myCall, date, band, mode, freq);
    }

    private static string NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();

    private readonly record struct DuplicateProbe(string Call, string MyCall, DateTime QsoDate, string Band, string Mode, decimal Freq);
}

internal readonly record struct QsoImportDuplicateFilterResult(IReadOnlyList<Qso> Accepted, int DuplicateCount);


