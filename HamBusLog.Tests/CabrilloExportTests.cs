namespace HamBusLog.Tests;

using HamBusLog.Data;
using HamBusLog.Models;
using HamBusLog.ViewModels;
using HamBusLog.Wa1gonLib.Models;
using System.Text.Json;

[Collection("Config file tests")]
public sealed class CabrilloExportTests : IDisposable
{
    private static readonly SemaphoreSlim ConfigLock = new(1, 1);

    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "hambuslog-tests", Guid.NewGuid().ToString("N"));
    private readonly string _configPath = AppConfigurationStore.GetConfigFilePath();
    private readonly bool _hadOriginalConfig;
    private readonly string? _originalConfigContent;
    private bool _disposed;
    private bool _lockHeld;

    public CabrilloExportTests()
    {
        ConfigLock.Wait();
        _lockHeld = true;
        Directory.CreateDirectory(_tempDirectory);

        if (File.Exists(_configPath))
        {
            _hadOriginalConfig = true;
            _originalConfigContent = File.ReadAllText(_configPath);
        }

        var configDirectory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(configDirectory))
            Directory.CreateDirectory(configDirectory);
    }

    [Fact]
    public async Task ExportArrlFieldDayToFileAsync_UsesDigitalModeNameAndAddsBonusPoints()
    {
        var databasePath = Path.Combine(_tempDirectory, "fieldday.db");
        var outputPath = Path.Combine(_tempDirectory, "fieldday.cab");
        var connectionString = $"Data Source={databasePath}";
        var now = DateTime.UtcNow;

        await using (var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, connectionString))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            db.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "W1AW",
                StationCallSign = "K1ABC",
                QsoDate = now,
                Mode = "FT8",
                ContestId = "ARRL-FIELD-DAY",
                Country = "USA",
                State = "CT",
                Freq = 14.074m,
                Band = "20M",
                RstSent = string.Empty,
                RstRcvd = string.Empty,
                Details =
                [
                    new QsoDetail { FieldName = "Class", FieldValue = "1D" },
                    new QsoDetail { FieldName = "Section", FieldValue = "CT" }
                ]
            });

            await db.SaveChangesAsync();
        }

        var config = new AppConfiguration();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        profile.ConnectionString = connectionString;
        profile.StationCallSign = "K1ABC";
        profile.MyStateProvince = "MA";
        profile.MyFieldDayClass = "2A";
        profile.MyFieldDaySection = "EMA";
        AppConfigurationStore.Save(config);

        var contest = new CabrilloContestDefinition(
            "ARRL-FD",
            "ARRL Field Day",
            "ARRL-FIELD-DAY",
            ["ARRL-FIELD-DAY", "ARRL-FD"],
            "ARRL-FD",
            []);
        var settings = new CabrilloExportSettings(new Dictionary<string, string>(), 100);

        var exportedCount = await CabrilloExportService.ExportToFileAsync(outputPath, contest, settings);
        var contents = await File.ReadAllTextAsync(outputPath);

        Assert.Equal(1, exportedCount);
        Assert.Contains("CLAIMED-SCORE: 102", contents);
        Assert.Contains("DIGITAL", contents);
        Assert.DoesNotContain(" DG ", contents);
    }

    [Fact]
    public void ArqpContestCatalog_UsesDropdownsAndComputedClaimedScore()
    {
        var assetPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Assets/cabrillo-contests.json"));
        var json = File.ReadAllText(assetPath);
        var config = JsonSerializer.Deserialize<CabrilloContestCatalogConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(config);

        var contest = config!.Contests.Single(contest => contest.Key == "ARQP");
        var categoryPower = contest.HeaderFields.Single(field => field.Key == "CATEGORY-POWER");
        var categoryMode = contest.HeaderFields.Single(field => field.Key == "CATEGORY-MODE");
        var categoryClass = contest.HeaderFields.Single(field => field.Key == "CATEGORY-CLASS");
        var claimedScore = contest.HeaderFields.Single(field => field.Key == "CLAIMED-SCORE");

        Assert.Collection(categoryPower.Options,
            option => Assert.Equal("QRP", option),
            option => Assert.Equal("LOW", option),
            option => Assert.Equal("HIGH", option));
        Assert.Collection(categoryMode.Options,
            option => Assert.Equal("CW", option),
            option => Assert.Equal("SSB", option),
            option => Assert.Equal("FM", option),
            option => Assert.Equal("RTTY", option),
            option => Assert.Equal("DIGITAL", option),
            option => Assert.Equal("MIXED", option));
        Assert.Equal("computed", claimedScore.InputType);
        Assert.True(categoryClass.IsUppercase);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_hadOriginalConfig)
                File.WriteAllText(_configPath, _originalConfigContent ?? string.Empty);
            else if (File.Exists(_configPath))
                File.Delete(_configPath);
        }
        finally
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                    Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup.
            }

            if (_lockHeld)
            {
                ConfigLock.Release();
                _lockHeld = false;
            }
        }
    }
}











