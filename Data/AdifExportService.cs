namespace HamBusLog.Data;

public static class AdifExportService
{
    public static async Task<int> ExportToFileAsync(
        string filePath,
        AdifImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Export file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        var importOptions = ResolveOptions(options);

        await using var db = HamBusLogDbContextFactory.Create(importOptions.Provider, importOptions.ConnectionString);
        var qsos = await db.Qsos
            .Include(x => x.Details)
            .Include(x => x.QslInfo)
            .OrderBy(x => x.QsoDate)
            .ToListAsync(cancellationToken);

        var adif = HamBusLog.Wa1gonLib.Adif.AdifWriter.WriteToAdif(qsos);
        await File.WriteAllTextAsync(fullPath, adif, cancellationToken);
        return qsos.Count;
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
}

