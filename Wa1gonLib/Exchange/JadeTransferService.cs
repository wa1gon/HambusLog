namespace HamBusLog.Wa1gonLib.Exchange;

using System.Text.Json.Nodes;
using HamBusLog.Data;
using HamBusLog.Wa1gonLib.Models;

public static class JadeTransferService
{
    public static async Task ExportExampleToFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Export file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        var recordArray = new JsonArray
        {
            JsonNode.Parse(ToJadeRecord(CreateExampleQso()))
        };

        var root = CreateRootDocument(recordArray);
        await File.WriteAllTextAsync(fullPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }

    public static async Task ExportSchemaToFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Export file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        var root = CreateRootDocument(new JsonArray());
        await File.WriteAllTextAsync(fullPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }

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

        var records = qsos.Select(ToJadeRecord).ToList();
        var recordArray = new JsonArray();
        foreach (var record in records)
            recordArray.Add(JsonNode.Parse(record));

        var root = CreateRootDocument(recordArray);

        await File.WriteAllTextAsync(fullPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        return qsos.Count;
    }

    private static JsonObject CreateRootDocument(JsonArray records)
    {
        return new JsonObject
        {
            ["format"] = "JADE",
            ["version"] = "1.0",
            ["schema"] = CreateSchemaDocument(),
            ["records"] = records
        };
    }

    private static JsonObject CreateSchemaDocument()
    {
        return new JsonObject
        {
            ["required"] = new JsonArray("UUID", "CALL", "MY_CALL", "QSO_DATE", "BAND_OR_FREQ"),
            ["date_format"] = "yyyyMMdd",
            ["time_format"] = "HHmm or HHmmss",
            ["freq_format"] = "MHz decimal string",
            ["adif_field_names"] = true,
            ["uuid_required"] = true,
            ["notes"] = new JsonArray(
                "JADE uses ADIF field names whenever possible.",
                "UUID is required in JADE and may also be accepted as GUID on import.",
                "At least one of BAND or FREQ must be present in each record.")
        };
    }

    private static Qso CreateExampleQso()
    {
        return new Qso
        {
            Id = Guid.Parse("2f3dcb4f-0adb-4dfc-bf95-72d11b3761e4"),
            Call = "JA1ABC",
            MyCall = "N0CALL",
            QsoDate = DateTime.SpecifyKind(new DateTime(2026, 5, 2, 18, 30, 15), DateTimeKind.Utc),
            Band = "20M",
            Freq = 14.074m,
            Mode = "FT8",
            Country = "Japan",
            Dxcc = 339,
            RstSent = "-10",
            RstRcvd = "-08",
            ContestId = string.Empty,
            Details = [new QsoDetail { FieldName = "COMMENT", FieldValue = "Example JADE record" }]
        };
    }

    public static async Task<int> ImportFromFileAsync(
        string filePath,
        AdifImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Import file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("JADE file was not found.", fullPath);

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var records = ParseRecords(json);
        if (records.Count == 0)
            return 0;

        var qsos = records.Select((record, index) => FromJadeRecord(record, index + 1)).ToList();
        PrepareForPersistence(qsos);

        var importOptions = ResolveOptions(options);
        await using var db = HamBusLogDbContextFactory.Create(importOptions.Provider, importOptions.ConnectionString);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        var duplicateFilter = await QsoImportDuplicateDetector.FilterNewQsosAsync(db, qsos, cancellationToken);
        if (duplicateFilter.Accepted.Count > 0)
        {
            await db.Qsos.AddRangeAsync(duplicateFilter.Accepted, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return duplicateFilter.Accepted.Count;
    }

    private static List<Dictionary<string, string>> ParseRecords(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (Exception ex)
        {
            throw new JadeValidationException("JADE file is not valid JSON.", [$"Invalid JSON: {ex.Message}"]);
        }

        if (root is not JsonObject obj)
            throw new JadeValidationException("Invalid JADE schema.", ["Root object must contain format, version, and records."]);

        var format = obj["format"]?.ToString();
        if (!string.Equals(format, "JADE", StringComparison.OrdinalIgnoreCase))
            throw new JadeValidationException("Invalid JADE schema.", ["Field 'format' must be 'JADE'."]);

        var version = obj["version"]?.ToString();
        if (!string.Equals(version, "1.0", StringComparison.OrdinalIgnoreCase))
            throw new JadeValidationException("Unsupported JADE version.", ["Field 'version' must be '1.0'."]);

        if (obj["records"] is not JsonArray array)
            throw new JadeValidationException("Invalid JADE schema.", ["Field 'records' must be an array."]);

        var list = new List<Dictionary<string, string>>();
        var errors = new List<string>();
        foreach (var node in array)
        {
            var recordNumber = list.Count + 1;
            if (node is not JsonObject item)
            {
                errors.Add($"Record {recordNumber}: entry is not a JSON object.");
                continue;
            }

            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in item)
            {
                if (pair.Value is null)
                    continue;

                fields[pair.Key] = pair.Value.ToString();
            }

            ValidateRecord(fields, recordNumber, errors);
            list.Add(fields);
        }

        if (errors.Count > 0)
            throw new JadeValidationException("JADE validation failed.", errors);

        return list;
    }

    private static void ValidateRecord(Dictionary<string, string> fields, int recordNumber, ICollection<string> errors)
    {
        fields.TryGetValue("UUID", out var uuid);
        fields.TryGetValue("GUID", out var guid);
        var idValue = string.IsNullOrWhiteSpace(uuid) ? guid : uuid;

        if (string.IsNullOrWhiteSpace(idValue))
            errors.Add($"Record {recordNumber}: UUID/GUID is required.");
        else if (!Guid.TryParse(idValue, out _))
            errors.Add($"Record {recordNumber}: UUID/GUID '{idValue}' is not a valid GUID.");

        if (!HasValue(fields, "CALL"))
            errors.Add($"Record {recordNumber}: CALL is required.");

        if (!HasValue(fields, "MY_CALL"))
            errors.Add($"Record {recordNumber}: MY_CALL is required.");

        if (!fields.TryGetValue("QSO_DATE", out var qsoDate) || string.IsNullOrWhiteSpace(qsoDate))
        {
            errors.Add($"Record {recordNumber}: QSO_DATE is required.");
        }
        else if (!DateTime.TryParseExact(qsoDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            errors.Add($"Record {recordNumber}: QSO_DATE '{qsoDate}' must use yyyyMMdd format.");
        }

        if (fields.TryGetValue("TIME_ON", out var timeOn) && !string.IsNullOrWhiteSpace(timeOn)
            && !TryParseAdifTime(timeOn, out _))
        {
            errors.Add($"Record {recordNumber}: TIME_ON '{timeOn}' must use HHmm or HHmmss format.");
        }

        var hasBand = HasValue(fields, "BAND");
        var hasFreq = HasValue(fields, "FREQ");
        if (!hasBand && !hasFreq)
        {
            errors.Add($"Record {recordNumber}: BAND and/or FREQ is required.");
        }
        else if (hasFreq)
        {
            var freqText = fields["FREQ"];
            if (!decimal.TryParse(freqText, NumberStyles.Float, CultureInfo.InvariantCulture, out var freq) || freq <= 0)
                errors.Add($"Record {recordNumber}: FREQ '{freqText}' is invalid.");
        }
    }

    private static bool HasValue(IReadOnlyDictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);

    private static bool TryParseAdifTime(string value, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (!Regex.IsMatch(trimmed, @"^\d{4}(\d{2})?$") )
            return false;

        var hour = int.Parse(trimmed[..2], CultureInfo.InvariantCulture);
        var minute = int.Parse(trimmed.Substring(2, 2), CultureInfo.InvariantCulture);
        var second = trimmed.Length == 6
            ? int.Parse(trimmed.Substring(4, 2), CultureInfo.InvariantCulture)
            : 0;

        if (hour is < 0 or > 23 || minute is < 0 or > 59 || second is < 0 or > 59)
            return false;

        time = new TimeSpan(hour, minute, second);
        return true;
    }

    private static string ToJadeRecord(Qso qso)
    {
        var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UUID"] = qso.Id.ToString(),
            ["CALL"] = qso.Call,
            ["MY_CALL"] = qso.MyCall,
            ["QSO_DATE"] = qso.QsoDate.ToUniversalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            ["TIME_ON"] = qso.QsoDate.ToUniversalTime().ToString("HHmmss", CultureInfo.InvariantCulture),
            ["BAND"] = qso.Band,
            ["FREQ"] = qso.Freq == 0m ? null : qso.Freq.ToString("0.000", CultureInfo.InvariantCulture),
            ["MODE"] = qso.Mode,
            ["COUNTRY"] = qso.Country,
            ["STATE"] = qso.State,
            ["DXCC"] = qso.Dxcc > 0 ? qso.Dxcc.ToString(CultureInfo.InvariantCulture) : null,
            ["RST_SENT"] = qso.RstSent,
            ["RST_RCVD"] = qso.RstRcvd,
            ["CONTEST-ID"] = qso.ContestId
        };

        foreach (var detail in qso.Details)
        {
            if (string.IsNullOrWhiteSpace(detail.FieldName) || string.IsNullOrWhiteSpace(detail.FieldValue))
                continue;

            var key = detail.FieldName.Trim().ToUpperInvariant();
            if (!record.ContainsKey(key))
                record[key] = detail.FieldValue;
        }

        foreach (var qsl in qso.QslInfo)
        {
            var service = qsl.QslService?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(service))
                continue;

            switch (service)
            {
                case "LOTW":
                    record["LOTW_QSL_SENT"] = qsl.QslSent ? "Y" : "N";
                    record["LOTW_QSL_RCVD"] = qsl.QslReceived ? "Y" : "N";
                    break;
                case "EQSL":
                    record["EQSL_QSL_SENT"] = qsl.QslSent ? "Y" : "N";
                    record["EQSL_QSL_RCVD"] = qsl.QslReceived ? "Y" : "N";
                    break;
                default:
                    record["QSL_SENT"] = qsl.QslSent ? "Y" : "N";
                    record["QSL_RCVD"] = qsl.QslReceived ? "Y" : "N";
                    break;
            }
        }

        return JsonSerializer.Serialize(record.Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value));
    }

    private static Qso FromJadeRecord(Dictionary<string, string> fields, int recordNumber)
    {
        var qso = new Qso();
        var qslInfos = new Dictionary<string, QsoQslInfo>(StringComparer.OrdinalIgnoreCase);
        DateTime? qsoDate = null;
        TimeSpan? qsoTime = null;

        foreach (var field in fields)
        {
            var name = field.Key.ToUpperInvariant();
            var value = field.Value;

            QsoQslInfo? GetOrCreateQsl(string service)
            {
                if (qslInfos.TryGetValue(service, out var existing))
                    return existing;

                var created = new QsoQslInfo { QslService = service };
                qslInfos[service] = created;
                return created;
            }

            switch (name)
            {
                case "UUID":
                case "GUID":
                    if (Guid.TryParse(value, out var parsedId))
                        qso.Id = parsedId;
                    break;
                case "CALL": qso.Call = value; break;
                case "MY_CALL": qso.MyCall = value; break;
                case "BAND": qso.Band = value; break;
                case "MODE": qso.Mode = value; break;
                case "COUNTRY": qso.Country = value; break;
                case "STATE": qso.State = value; break;
                case "DXCC": qso.Dxcc = int.TryParse(value, out var dxcc) ? dxcc : 0; break;
                case "RST_SENT": qso.RstSent = value; break;
                case "RST_RCVD": qso.RstRcvd = value; break;
                case "CONTEST-ID": qso.ContestId = value; break;
                case "FREQ":
                    if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var freq))
                        qso.Freq = freq;
                    break;
                case "QSO_DATE":
                    if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        qsoDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    break;
                case "TIME_ON":
                    if (TryParseAdifTime(value, out var time))
                        qsoTime = time;
                    break;
                case "LOTW_QSL_SENT": GetOrCreateQsl("LOTW")!.QslSent = value.Equals("Y", StringComparison.OrdinalIgnoreCase); break;
                case "LOTW_QSL_RCVD": GetOrCreateQsl("LOTW")!.QslReceived = value.Equals("Y", StringComparison.OrdinalIgnoreCase); break;
                case "EQSL_QSL_SENT": GetOrCreateQsl("EQSL")!.QslSent = value.Equals("Y", StringComparison.OrdinalIgnoreCase); break;
                case "EQSL_QSL_RCVD": GetOrCreateQsl("EQSL")!.QslReceived = value.Equals("Y", StringComparison.OrdinalIgnoreCase); break;
                case "QSL_SENT": GetOrCreateQsl("DIRECT")!.QslSent = value.Equals("Y", StringComparison.OrdinalIgnoreCase); break;
                case "QSL_RCVD": GetOrCreateQsl("DIRECT")!.QslReceived = value.Equals("Y", StringComparison.OrdinalIgnoreCase); break;
                default:
                    qso.Details.Add(new QsoDetail { FieldName = field.Key, FieldValue = value });
                    break;
            }
        }

        if (qso.Id == Guid.Empty)
            throw new JadeValidationException("JADE validation failed.", [$"Record {recordNumber}: UUID/GUID is required."]);

        if (qsoDate.HasValue)
        {
            var dateTime = qsoDate.Value.Date;
            if (qsoTime.HasValue)
                dateTime = dateTime.Add(qsoTime.Value);
            qso.QsoDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        else
        {
            throw new JadeValidationException("JADE validation failed.", [$"Record {recordNumber}: QSO_DATE is required."]);
        }

        qso.QslInfo = qslInfos.Values.ToList();
        return qso;
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




