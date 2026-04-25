namespace HamBusLog.Data;

public static class AdifImportService
{
    public static async Task<AdifImportResult> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("ADIF file path is required.", nameof(filePath));

        var parsed = HbLibrary.Adif.AdifReader.ReadFromAdifFile(filePath);

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var connectionString = string.IsNullOrWhiteSpace(profile.ConnectionString)
            ? "Data Source=hambuslog.db"
            : profile.ConnectionString;

        await using var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, connectionString);
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;
        foreach (var qso in parsed)
        {
            if (qso.Id == Guid.Empty)
                qso.Id = Guid.NewGuid();

            if (qso.LastUpdate == default)
                qso.LastUpdate = nowUtc;

            qso.Details ??= new List<QsoDetail>();
            qso.QslInfo ??= new List<QsoQslInfo>();

            foreach (var detail in qso.Details)
            {
                if (detail.QsoId == Guid.Empty)
                    detail.QsoId = qso.Id;
            }

            foreach (var qsl in qso.QslInfo)
            {
                if (qsl.QsoId == Guid.Empty)
                    qsl.QsoId = qso.Id;
            }
        }

        await db.Qsos.AddRangeAsync(parsed, cancellationToken);
        var saved = await db.SaveChangesAsync(cancellationToken);

        return new AdifImportResult(parsed.Count, saved, filePath);
    }
}

public readonly record struct AdifImportResult(int ParsedCount, int SavedChanges, string FilePath);


