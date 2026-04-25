namespace HamBusLog.Data;

public static class AdifImportService
{
    public static async Task<AdifImportResult> ImportFromFileAsync(
        string filePath,
        AdifImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("ADIF file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("ADIF file was not found.", fullPath);

        var parsed = HbLibrary.Adif.AdifReader.ReadFromAdifFile(fullPath) ?? [];
        if (parsed.Count == 0)
            return new AdifImportResult(0, 0, fullPath);

        PrepareForPersistence(parsed);

        var importOptions = ResolveOptions(options);

        await using var db = HamBusLogDbContextFactory.Create(importOptions.Provider, importOptions.ConnectionString);
        await db.Database.EnsureCreatedAsync(cancellationToken);

        await db.Qsos.AddRangeAsync(parsed, cancellationToken);
        var saved = await db.SaveChangesAsync(cancellationToken);

        return new AdifImportResult(parsed.Count, saved, fullPath);
    }

    private static AdifImportOptions ResolveOptions(AdifImportOptions? options)
    {
        if (options is { ConnectionString: { Length: > 0 } })
            return options.Value;

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var connectionString = string.IsNullOrWhiteSpace(profile.ConnectionString)
            ? "Data Source=hambuslog.db"
            : profile.ConnectionString;

        return new AdifImportOptions(options?.Provider ?? DatabaseProvider.Sqlite, connectionString);
    }

    private static void PrepareForPersistence(IEnumerable<Qso> qsos)
    {
        var nowUtc = DateTime.UtcNow;
        foreach (var qso in qsos)
        {
            if (qso.Id == Guid.Empty)
                qso.Id = Guid.NewGuid();

            if (qso.LastUpdate == default)
                qso.LastUpdate = nowUtc;

            qso.Details ??= [];
            qso.QslInfo ??= [];

            foreach (var detail in qso.Details)
                detail.QsoId = qso.Id;

            foreach (var qsl in qso.QslInfo)
                qsl.QsoId = qso.Id;
        }
    }
}

public readonly record struct AdifImportOptions(DatabaseProvider Provider = DatabaseProvider.Sqlite, string? ConnectionString = null)
{
    public string ConnectionString { get; } = string.IsNullOrWhiteSpace(ConnectionString)
        ? string.Empty
        : ConnectionString;
}

public readonly record struct AdifImportResult(int ParsedCount, int SavedChanges, string FilePath);


