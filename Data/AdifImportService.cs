using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.Data;

public static class AdifImportService
{
    public static async Task<AdifImportResult> ImportFromFileAsync(
        string filePath,
        AdifImportOptions? options = null,
        IProgress<AdifImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("ADIF file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("ADIF file was not found.", fullPath);

        progress?.Report(AdifImportProgress.Starting(fullPath));

        var scannedRecords = await CountRecordsAsync(fullPath, progress, cancellationToken);
        progress?.Report(AdifImportProgress.Parsing(fullPath, scannedRecords));

        var parsed = HamBusLog.Wa1gonLib.Adif.AdifReader.ReadFromAdifFile(fullPath) ?? [];
        if (parsed.Count == 0)
        {
            progress?.Report(AdifImportProgress.Completed(fullPath, 0, 0));
            return new AdifImportResult(0, 0, fullPath);
        }

        PrepareForPersistence(parsed);

        var importOptions = ResolveOptions(options);

        await using var db = HamBusLogDbContextFactory.Create(importOptions.Provider, importOptions.ConnectionString);
        await db.Database.EnsureCreatedAsync(cancellationToken);

        progress?.Report(AdifImportProgress.Saving(fullPath, parsed.Count));
        await db.Qsos.AddRangeAsync(parsed, cancellationToken);
        var saved = await db.SaveChangesAsync(cancellationToken);

        progress?.Report(AdifImportProgress.Completed(fullPath, parsed.Count, saved));

        return new AdifImportResult(parsed.Count, saved, fullPath);
    }

    private static async Task<int> CountRecordsAsync(
        string filePath,
        IProgress<AdifImportProgress>? progress,
        CancellationToken cancellationToken)
    {
        const string marker = "<EOR>";
        const int bufferSize = 4096;

        var fileLength = new FileInfo(filePath).Length;
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        using var reader = new StreamReader(stream);

        var buffer = new char[bufferSize];
        var carry = string.Empty;
        var recordsFound = 0;
        var lastReported = -1;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
                break;

            var chunk = carry + new string(buffer, 0, read);
            var searchStart = 0;
            while (true)
            {
                var index = chunk.IndexOf(marker, searchStart, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    break;

                recordsFound++;
                searchStart = index + marker.Length;
            }

            carry = chunk.Length >= marker.Length - 1
                ? chunk[^Math.Min(marker.Length - 1, chunk.Length)..]
                : chunk;

            if (recordsFound != lastReported)
            {
                lastReported = recordsFound;
                var progressFraction = fileLength <= 0
                    ? 0d
                    : Math.Clamp(reader.BaseStream.Position / (double)fileLength, 0d, 1d);

                progress?.Report(AdifImportProgress.Scanning(filePath, recordsFound, progressFraction));
            }
        }

        progress?.Report(AdifImportProgress.Scanning(filePath, recordsFound, 1d));
        return recordsFound;
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

public enum AdifImportStage
{
    Starting,
    Scanning,
    Parsing,
    Saving,
    Completed
}

public readonly record struct AdifImportProgress(
    AdifImportStage Stage,
    string FilePath,
    string StatusText,
    int RecordsRead = 0,
    int? TotalRecords = null,
    int SavedChanges = 0,
    bool IsIndeterminate = true,
    double ProgressFraction = 0d)
{
    public static AdifImportProgress Starting(string filePath) =>
        new(AdifImportStage.Starting, filePath, $"Opening {Path.GetFileName(filePath)}...", 0, null, 0, true, 0d);

    public static AdifImportProgress Scanning(string filePath, int recordsRead, double progressFraction) =>
        new(AdifImportStage.Scanning, filePath, $"Scanning ADIF records in {Path.GetFileName(filePath)}...", recordsRead, null, 0, false, progressFraction);

    public static AdifImportProgress Parsing(string filePath, int recordsRead) =>
        new(AdifImportStage.Parsing, filePath, $"Parsing {recordsRead:N0} record(s)...", recordsRead, recordsRead, 0, true, 0d);

    public static AdifImportProgress Saving(string filePath, int recordsRead) =>
        new(AdifImportStage.Saving, filePath, $"Saving {recordsRead:N0} record(s) to the database...", recordsRead, recordsRead, 0, true, 0d);

    public static AdifImportProgress Completed(string filePath, int recordsRead, int savedChanges) =>
        new(AdifImportStage.Completed, filePath, $"Imported {recordsRead:N0} record(s) from {Path.GetFileName(filePath)}.", recordsRead, recordsRead, savedChanges, false, 1d);
}

