using HamBusLog.Data;
using HamBusLog.Wa1gonLib.Exchange;
using HamBusLog.Wa1gonLib.Adif;
using HamBusLog.Wa1gonLib.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Text.Json.Nodes;

namespace HamBusLog.Tests;

public sealed class TransferServicesTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "hambuslog-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void AdifWriter_IncludesUuid_AndReaderParsesIt()
    {
        var id = Guid.NewGuid();
        var qso = new Qso
        {
            Id = id,
            Call = "W1AW",
            QsoDate = DateTime.SpecifyKind(new DateTime(2026, 5, 2, 12, 30, 0), DateTimeKind.Utc),
            Band = "20M",
            Mode = "SSB",
            Freq = 14.250m,
            Details = []
        };

        var adif = AdifWriter.WriteToAdif([qso]);
        Assert.Contains("<UUID:", adif, StringComparison.OrdinalIgnoreCase);

        var parsed = AdifReader.ReadFromString(adif);
        Assert.Single(parsed);
        Assert.Equal(id, parsed[0].Id);
    }

    [Fact]
    public async Task JadeExportImport_RoundTripsCoreFieldsAndUuid()
    {
        Directory.CreateDirectory(_tempDirectory);
        var srcDbPath = Path.Combine(_tempDirectory, "src.sqlite");
        var dstDbPath = Path.Combine(_tempDirectory, "dst.sqlite");
        var jadePath = Path.Combine(_tempDirectory, "sample.json");

        var sourceConn = $"Data Source={srcDbPath}";
        var targetConn = $"Data Source={dstDbPath}";

        var qsoId = Guid.NewGuid();
        await using (var src = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, sourceConn))
        {
            await src.Database.EnsureCreatedAsync();
            src.Qsos.Add(new Qso
            {
                Id = qsoId,
                Call = "JA1ABC",
                MyCall = "N0CALL",
                QsoDate = DateTime.SpecifyKind(new DateTime(2026, 5, 1, 1, 2, 3), DateTimeKind.Utc),
                Band = "20M",
                Mode = "CW",
                Freq = 14.025m,
                RstSent = "599",
                RstRcvd = "579",
                Details = [new QsoDetail { FieldName = "operator", FieldValue = "Darryl" }]
            });
            await src.SaveChangesAsync();
        }

        var exported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportToFileAsync(
            jadePath,
            new AdifImportOptions(DatabaseProvider.Sqlite, sourceConn));
        Assert.Equal(1, exported);

        var imported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(
            jadePath,
            new AdifImportOptions(DatabaseProvider.Sqlite, targetConn));
        Assert.Equal(1, imported);

        await using var dst = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, targetConn);
        var stored = await dst.Qsos.Include(x => x.Details).SingleAsync();
        Assert.Equal(qsoId, stored.Id);
        Assert.Equal("JA1ABC", stored.Call);
        Assert.Equal("20M", stored.Band);
        Assert.Equal("CW", stored.Mode);
        Assert.Equal(14.025m, stored.Freq);
    }

    [Fact]
    public async Task JadeImport_SkipsDuplicatesOnReimport()
    {
        Directory.CreateDirectory(_tempDirectory);
        var srcDbPath = Path.Combine(_tempDirectory, "src-dupe.sqlite");
        var dstDbPath = Path.Combine(_tempDirectory, "dst-dupe.sqlite");
        var jadePath = Path.Combine(_tempDirectory, "dupe.json");

        var sourceConn = $"Data Source={srcDbPath}";
        var targetConn = $"Data Source={dstDbPath}";

        await using (var src = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, sourceConn))
        {
            await src.Database.EnsureCreatedAsync();
            src.Qsos.Add(new Qso
            {
                Id = Guid.Parse("2f3dcb4f-0adb-4dfc-bf95-72d11b3761e4"),
                Call = "JA1ABC",
                MyCall = "N0CALL",
                QsoDate = DateTime.SpecifyKind(new DateTime(2026, 5, 1, 1, 2, 3), DateTimeKind.Utc),
                Band = "20M",
                Mode = "CW",
                Freq = 14.025m
            });
            await src.SaveChangesAsync();
        }

        await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportToFileAsync(
            jadePath,
            new AdifImportOptions(DatabaseProvider.Sqlite, sourceConn));

        var firstImported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(
            jadePath,
            new AdifImportOptions(DatabaseProvider.Sqlite, targetConn));
        var secondImported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(
            jadePath,
            new AdifImportOptions(DatabaseProvider.Sqlite, targetConn));

        Assert.Equal(1, firstImported);
        Assert.Equal(0, secondImported);

        await using var dst = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, targetConn);
        Assert.Equal(1, await dst.Qsos.CountAsync());
    }

    [Fact]
    public async Task JadeImport_RejectsInvalidSchemaVersion()
    {
        Directory.CreateDirectory(_tempDirectory);
        var jadePath = Path.Combine(_tempDirectory, "bad-version.json");
        await File.WriteAllTextAsync(jadePath, """
            {
              "format": "JADE",
              "version": "2.0",
              "records": []
            }
            """);

        var ex = await Assert.ThrowsAsync<HamBusLog.Wa1gonLib.Exchange.JadeValidationException>(() =>
            HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(jadePath));

        Assert.Contains("version", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ex.Errors, e => e.Contains("version", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task JadeImport_RejectsMalformedRequiredFieldsWithRecordErrors()
    {
        Directory.CreateDirectory(_tempDirectory);
        var jadePath = Path.Combine(_tempDirectory, "invalid-records.json");
        await File.WriteAllTextAsync(jadePath, """
            {
              "format": "JADE",
              "version": "1.0",
              "records": [
                {
                  "UUID": "not-a-guid",
                  "CALL": "",
                  "MY_CALL": "N0CALL",
                  "QSO_DATE": "2026-05-02",
                  "TIME_ON": "250000",
                  "FREQ": "abc"
                }
              ]
            }
            """);

        var ex = await Assert.ThrowsAsync<HamBusLog.Wa1gonLib.Exchange.JadeValidationException>(() =>
            HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(jadePath));

        Assert.Contains(ex.Errors, e => e.Contains("Record 1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ex.Errors, e => e.Contains("UUID", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ex.Errors, e => e.Contains("CALL", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ex.Errors, e => e.Contains("QSO_DATE", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ex.Errors, e => e.Contains("TIME_ON", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ex.Errors, e => e.Contains("FREQ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task JadeExportSchemaOnly_WritesSchemaAndEmptyRecords()
    {
        Directory.CreateDirectory(_tempDirectory);
        var jadePath = Path.Combine(_tempDirectory, "jade-schema-only.json");

        await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportSchemaToFileAsync(jadePath);

        var json = await File.ReadAllTextAsync(jadePath);
        var root = JsonNode.Parse(json)!.AsObject();

        Assert.Equal("JADE", root["format"]?.ToString());
        Assert.Equal("1.0", root["version"]?.ToString());
        Assert.NotNull(root["schema"]);
        Assert.NotNull(root["records"]);
        Assert.Empty(root["records"]!.AsArray());

        var required = root["schema"]!["required"]!.AsArray().Select(x => x!.ToString()).ToList();
        Assert.Contains("UUID", required);
        Assert.Contains("CALL", required);
        Assert.Contains("MY_CALL", required);
        Assert.Contains("QSO_DATE", required);
        Assert.Contains("BAND_OR_FREQ", required);
    }

    [Fact]
    public async Task JadeExportExample_WritesOneSampleRecord()
    {
        Directory.CreateDirectory(_tempDirectory);
        var jadePath = Path.Combine(_tempDirectory, "jade-example-record.json");

        await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportExampleToFileAsync(jadePath);

        var json = await File.ReadAllTextAsync(jadePath);
        var root = JsonNode.Parse(json)!.AsObject();
        var records = root["records"]!.AsArray();

        Assert.Single(records);

        var record = records[0]!.AsObject();
        Assert.Equal("JA1ABC", record["CALL"]?.ToString());
        Assert.Equal("N0CALL", record["MY_CALL"]?.ToString());
        Assert.Equal("20260502", record["QSO_DATE"]?.ToString());
        Assert.Equal("183015", record["TIME_ON"]?.ToString());
        Assert.Equal("20M", record["BAND"]?.ToString());
        Assert.Equal("14.074", record["FREQ"]?.ToString());
        Assert.Equal("FT8", record["MODE"]?.ToString());
        Assert.Equal("Japan", record["COUNTRY"]?.ToString());
        Assert.Equal("339", record["DXCC"]?.ToString());
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
            // Best-effort temp cleanup.
        }
    }
}





