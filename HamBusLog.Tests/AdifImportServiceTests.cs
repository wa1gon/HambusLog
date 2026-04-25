using HamBusLog.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HamBusLog.Tests;

public sealed class AdifImportServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "hambuslog-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ImportFromFileAsync_ThrowsWhenFileDoesNotExist()
    {
        Directory.CreateDirectory(_tempDirectory);
        var missingPath = Path.Combine(_tempDirectory, "missing.adi");
        var dbPath = Path.Combine(_tempDirectory, "missing.sqlite");

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            AdifImportService.ImportFromFileAsync(missingPath, new AdifImportOptions(ConnectionString: $"Data Source={dbPath}")));

        Assert.Equal(Path.GetFullPath(missingPath), ex.FileName);
    }

    [Fact]
    public async Task ImportFromFileAsync_ReturnsZeroCountsForEmptyAdif()
    {
        Directory.CreateDirectory(_tempDirectory);
        var adifPath = Path.Combine(_tempDirectory, "empty.adi");
        var dbPath = Path.Combine(_tempDirectory, "empty.sqlite");
        await File.WriteAllTextAsync(adifPath, "Header <eoh>\n");

        var result = await AdifImportService.ImportFromFileAsync(
            adifPath,
            new AdifImportOptions(ConnectionString: $"Data Source={dbPath}"));

        Assert.Equal(0, result.ParsedCount);
        Assert.Equal(0, result.SavedChanges);
        Assert.Equal(Path.GetFullPath(adifPath), result.FilePath);
    }

    [Fact]
    public async Task ImportFromFileAsync_PersistsParsedQsoAndRelatedEntities()
    {
        Directory.CreateDirectory(_tempDirectory);
        var adifPath = Path.Combine(_tempDirectory, "sample.adi");
        var dbPath = Path.Combine(_tempDirectory, "sample.sqlite");
        await File.WriteAllTextAsync(adifPath, CreateSampleAdif());
        var progressUpdates = new List<AdifImportProgress>();
        var progress = new Progress<AdifImportProgress>(update => progressUpdates.Add(update));

        var result = await AdifImportService.ImportFromFileAsync(
            adifPath,
            new AdifImportOptions(ConnectionString: $"Data Source={dbPath}"),
            progress);

        Assert.Equal(1, result.ParsedCount);
        Assert.True(result.SavedChanges >= 1);
        Assert.Contains(progressUpdates, x => x.Stage == AdifImportStage.Scanning && x.RecordsRead >= 1);
        Assert.Contains(progressUpdates, x => x.Stage == AdifImportStage.Completed && x.RecordsRead == 1);

        await using var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, $"Data Source={dbPath}");
        var storedQso = await db.Qsos
            .Include(q => q.QslInfo)
            .SingleAsync();

        Assert.Equal("W1AW", storedQso.Call);
        Assert.NotEqual(Guid.Empty, storedQso.Id);
        Assert.NotEqual(default, storedQso.LastUpdate);
        Assert.Single(storedQso.QslInfo);
        Assert.Equal(storedQso.Id, storedQso.QslInfo.Single().QsoId);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for temp files.
        }
    }

    private static string CreateSampleAdif() =>
        "Sample Header <eoh>\n<CALL:4>W1AW <QSO_DATE:8>20260425 <TIME_ON:6>123000 <BAND:3>20M <MODE:3>SSB <RST_SENT:2>59 <RST_RCVD:2>59 <EOR>\n";
}


