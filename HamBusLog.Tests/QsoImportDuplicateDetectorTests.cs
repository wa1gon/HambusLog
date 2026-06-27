using HamBusLog.Data;
using HamBusLog.Wa1gonLib.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HamBusLog.Tests;

public sealed class QsoImportDuplicateDetectorTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hambuslog-dups-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task IsDuplicateAsync_ReturnsTrue_ForMatchingSignature()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        var existing = BuildQso();
        db.Qsos.Add(existing);
        await db.SaveChangesAsync();

        var incoming = BuildQso();
        var isDuplicate = await QsoImportDuplicateDetector.IsDuplicateAsync(db, incoming);

        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicateAsync_ReturnsFalse_WhenTimeDiffers()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        var existing = BuildQso();
        db.Qsos.Add(existing);
        await db.SaveChangesAsync();

        var incoming = BuildQso();
        incoming.QsoDate = incoming.QsoDate.AddSeconds(1);

        var isDuplicate = await QsoImportDuplicateDetector.IsDuplicateAsync(db, incoming);

        Assert.False(isDuplicate);
    }

    private HamBusLogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HamBusLogDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        return new HamBusLogDbContext(options);
    }

    private static Qso BuildQso()
    {
        return new Qso
        {
            Id = Guid.NewGuid(),
            Call = "K1ABC",
            StationCallSign = "WA1GON",
            QsoDate = new DateTime(2026, 6, 27, 18, 30, 0, DateTimeKind.Utc),
            Band = "20M",
            Mode = "FT8",
            Freq = 14.074m
        };
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch
        {
        }
    }
}

