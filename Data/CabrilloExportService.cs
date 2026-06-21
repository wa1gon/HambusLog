using System.Text;

namespace HamBusLog.Data;

public static class CabrilloExportService
{
    private const string ArqpExporterKey = "ARQP";
    private const string ArrlFieldDayExporterKey = "ARRL-FD";

    public static bool IsSupportedExporter(string? exporterKey)
    {
        if (string.IsNullOrWhiteSpace(exporterKey))
            return false;

        return string.Equals(exporterKey, ArqpExporterKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(exporterKey, ArrlFieldDayExporterKey, StringComparison.OrdinalIgnoreCase);
    }

    public static Task<int> ExportToFileAsync(
        string filePath,
        CabrilloContestDefinition contest,
        CabrilloExportSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        if (contest is null)
            throw new ArgumentNullException(nameof(contest));

        if (string.Equals(contest.ExporterKey, ArrlFieldDayExporterKey, StringComparison.OrdinalIgnoreCase))
            return ExportArrlFieldDayToFileAsync(filePath, contest, settings, cancellationToken);

        if (string.Equals(contest.ExporterKey, ArqpExporterKey, StringComparison.OrdinalIgnoreCase))
            return ExportArqpToFileAsync(filePath, contest, settings, cancellationToken);

        throw new InvalidOperationException($"No Cabrillo exporter is registered for '{contest.ExporterKey}'.");
    }

    public static async Task<int> ExportArqpToFileAsync(
        string filePath,
        CabrilloContestDefinition contest,
        CabrilloExportSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Export file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var connectionString = string.IsNullOrWhiteSpace(profile.ConnectionString)
            ? "Data Source=hambuslog.db"
            : profile.ConnectionString;

        var sinceUtc = ResolveContestStart(settings) ?? DateTime.UtcNow.AddMonths(-6);
        var untilUtc = ResolveContestEnd(settings);

        var contestIds = NormalizeContestIds(contest);

        await using var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, connectionString);
        var qsos = await db.Qsos
            .Include(x => x.Details)
            .Where(x => x.QsoDate >= sinceUtc
                        && x.ContestId != null
                        && contestIds.Contains(x.ContestId.ToUpper())
                        && (untilUtc == null || x.QsoDate <= untilUtc.Value))
            .OrderBy(x => x.QsoDate)
            .ThenBy(x => x.Call)
            .ToListAsync(cancellationToken);

        var cabrillo = BuildArqpCabrillo(profile, contest.AdifContestId, settings, qsos);
        await File.WriteAllTextAsync(fullPath, cabrillo, cancellationToken);
        return qsos.Count;
    }

    public static async Task<int> ExportArrlFieldDayToFileAsync(
        string filePath,
        CabrilloContestDefinition contest,
        CabrilloExportSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Export file path is required.", nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var connectionString = string.IsNullOrWhiteSpace(profile.ConnectionString)
            ? "Data Source=hambuslog.db"
            : profile.ConnectionString;

        var sinceUtc = ResolveContestStart(settings) ?? DateTime.UtcNow.AddMonths(-6);
        var untilUtc = ResolveContestEnd(settings);

        var contestIds = NormalizeContestIds(contest);

        await using var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, connectionString);
        var qsos = await db.Qsos
            .Include(x => x.Details)
            .Where(x => x.QsoDate >= sinceUtc
                        && x.ContestId != null
                        && contestIds.Contains(x.ContestId.ToUpper())
                        && (untilUtc == null || x.QsoDate <= untilUtc.Value))
            .OrderBy(x => x.QsoDate)
            .ThenBy(x => x.Call)
            .ToListAsync(cancellationToken);

        var cabrillo = BuildArrlFieldDayCabrillo(profile, contest.AdifContestId, settings, qsos);
        await File.WriteAllTextAsync(fullPath, cabrillo, cancellationToken);
        return qsos.Count;
    }

    private static string BuildArqpCabrillo(
        ConfigProfile profile,
        string contestId,
        CabrilloExportSettings? settings,
        IReadOnlyList<Qso> qsos)
    {
        var sb = new StringBuilder();
        var callSign = ResolveHeaderValue(settings, "CALLSIGN", profile.StationCallSign).ToUpperInvariant();
        var location = ResolveHeaderValue(settings, "LOCATION", profile.MyStateProvince).ToUpperInvariant();

        AppendHeader(sb, "START-OF-LOG", "3.1");
        AppendHeader(sb, "CREATED-BY", "HamBusLog");
        AppendHeader(sb, "CONTEST", contestId);
        AppendHeader(sb, "CALLSIGN", callSign);
        AppendHeader(sb, "CATEGORY-OPERATOR", NormalizeHeader(ResolveHeaderValue(settings, "CATEGORY-OPERATOR", "SINGLE-OP")));
        AppendHeader(sb, "CATEGORY-ASSISTED", NormalizeHeader(ResolveHeaderValue(settings, "CATEGORY-ASSISTED", "NON-ASSISTED")));
        AppendHeader(sb, "CATEGORY-BAND", NormalizeHeader(ResolveHeaderValue(settings, "CATEGORY-BAND", "ALL")));
        AppendHeader(sb, "CATEGORY-MODE", NormalizeHeader(ResolveHeaderValue(settings, "CATEGORY-MODE", "MIXED")));
        AppendHeader(sb, "CATEGORY-POWER", NormalizeHeader(ResolveHeaderValue(settings, "CATEGORY-POWER", "LOW")));
        AppendHeader(sb, "CATEGORY-CLASS", NormalizeHeader(ResolveHeaderValue(settings, "CATEGORY-CLASS", null)));
        AppendHeader(sb, "CATEGORY-TRANSMITTER", NormalizeHeader(ResolveHeaderValue(settings, "CATEGORY-TRANSMITTER", "ONE")));
        AppendHeader(sb, "LOCATION", location);
        AppendHeader(sb, "CLAIMED-SCORE", qsos.Count.ToString(CultureInfo.InvariantCulture));
        AppendHeader(sb, "OPERATORS", ResolveHeaderValue(settings, "OPERATORS", callSign));
        AppendHeader(sb, "CLUB", ResolveHeaderValue(settings, "CLUB", null));
        AppendHeader(sb, "ADDRESS", ResolveHeaderValue(settings, "ADDRESS", null));
        AppendHeader(sb, "EMAIL", ResolveHeaderValue(settings, "EMAIL", null));

        AppendSoapbox(sb, ResolveHeaderValue(settings, "SOAPBOX", null));

        foreach (var qso in qsos)
            sb.AppendLine(FormatArqpQsoLine(profile, qso));

        sb.AppendLine("END-OF-LOG:");
        return sb.ToString();
    }

    private static string BuildArrlFieldDayCabrillo(
        ConfigProfile profile,
        string contestId,
        CabrilloExportSettings? settings,
        IReadOnlyList<Qso> qsos)
    {
        var sb = new StringBuilder();
        var callSign = ResolveHeaderValue(settings, "CALLSIGN", profile.StationCallSign).ToUpperInvariant();
        var location = ResolveHeaderValue(settings, "LOCATION", profile.MyStateProvince).ToUpperInvariant();
        var category = ResolveHeaderValue(settings, "CATEGORY", profile.MyFieldDayClass).ToUpperInvariant();
        var arrlSection = ResolveHeaderValue(settings, "ARRL-SECTION", profile.MyFieldDaySection).ToUpperInvariant();
        var bonusPoints = settings?.BonusPoints ?? 0;

        AppendHeader(sb, "START-OF-LOG", "3.1");
        AppendHeader(sb, "CREATED-BY", "HamBusLog");
        AppendHeader(sb, "CONTEST", contestId);
        AppendHeader(sb, "CALLSIGN", callSign);
        AppendHeader(sb, "CATEGORY", category);
        AppendHeader(sb, "LOCATION", location);
        AppendHeader(sb, "ARRL-SECTION", arrlSection);
        AppendHeader(sb, "CLAIMED-SCORE", CalculateFieldDayScore(qsos, bonusPoints).ToString(CultureInfo.InvariantCulture));
        AppendHeader(sb, "OPERATORS", ResolveHeaderValue(settings, "OPERATORS", callSign));
        AppendHeader(sb, "NAME", ResolveHeaderValue(settings, "NAME", null));
        AppendHeader(sb, "ADDRESS", ResolveHeaderValue(settings, "ADDRESS", null));
        AppendHeader(sb, "ADDRESS-CITY", ResolveHeaderValue(settings, "ADDRESS-CITY", null));
        AppendHeader(sb, "ADDRESS-STATE-PROVINCE", ResolveHeaderValue(settings, "ADDRESS-STATE-PROVINCE", null));
        AppendHeader(sb, "ADDRESS-POSTALCODE", ResolveHeaderValue(settings, "ADDRESS-POSTALCODE", null));
        AppendHeader(sb, "ADDRESS-COUNTRY", ResolveHeaderValue(settings, "ADDRESS-COUNTRY", null));
        AppendHeader(sb, "CLUB", ResolveHeaderValue(settings, "CLUB", null));
        AppendHeader(sb, "EMAIL", ResolveHeaderValue(settings, "EMAIL", null));

        AppendSoapbox(sb, ResolveHeaderValue(settings, "SOAPBOX", null));

        foreach (var qso in qsos)
            sb.AppendLine(FormatArrlFieldDayQsoLine(profile, settings, qso));

        sb.AppendLine("END-OF-LOG:");
        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        sb.Append(key);
        sb.Append(": ");
        sb.AppendLine(value.Trim());
    }

    private static string FormatArqpQsoLine(ConfigProfile profile, Qso qso)
    {
        var utc = qso.QsoDate.Kind == DateTimeKind.Utc
            ? qso.QsoDate
            : qso.QsoDate.ToUniversalTime();

        var frequency = FormatFrequencyKhz(qso);
        var mode = NormalizeFieldDayCabrilloMode(qso.Mode);
        var date = utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var time = utc.ToString("HHmm", CultureInfo.InvariantCulture);
        var myCall = profile.StationCallSign.Trim().ToUpperInvariant();
        var rstSent = NormalizeRst(qso.RstSent, mode);
        var rstRcvd = NormalizeRst(qso.RstRcvd, mode);
        var sentExchange = BuildSentExchange(profile);
        var rcvdExchange = BuildReceivedExchange(qso);
        var hisCall = (qso.Call ?? string.Empty).Trim().ToUpperInvariant();

        return string.Format(CultureInfo.InvariantCulture,
            "QSO: {0,6} {1,-2} {2} {3} {4,-13} {5,-3} {6,-6} {7,-13} {8,-3} {9}",
            frequency,
            mode,
            date,
            time,
            myCall,
            rstSent,
            sentExchange,
            hisCall,
            rstRcvd,
            rcvdExchange);
    }

    private static string FormatArrlFieldDayQsoLine(ConfigProfile profile, CabrilloExportSettings? settings, Qso qso)
    {
        var utc = qso.QsoDate.Kind == DateTimeKind.Utc
            ? qso.QsoDate
            : qso.QsoDate.ToUniversalTime();

        var frequency = FormatFrequencyKhz(qso);
        var mode = NormalizeFieldDayCabrilloMode(qso.Mode);
        var date = utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var time = utc.ToString("HHmm", CultureInfo.InvariantCulture);
        var myCall = ResolveHeaderValue(settings, "CALLSIGN", profile.StationCallSign).ToUpperInvariant();
        var (myClass, mySection) = BuildFieldDaySentExchange(profile, settings);
        var (theirClass, theirSection) = BuildFieldDayReceivedExchange(qso);
        var hisCall = (qso.Call ?? string.Empty).Trim().ToUpperInvariant();

        return string.Format(CultureInfo.InvariantCulture,
            "QSO: {0,6} {1,-7} {2} {3} {4,-13} {5,-2} {6,-6} {7,-13} {8,-2} {9,-6}",
            frequency,
            mode,
            date,
            time,
            myCall,
            myClass,
            mySection,
            hisCall,
            theirClass,
            theirSection);
    }

    private static string BuildSentExchange(ConfigProfile profile)
    {
        var state = profile.MyStateProvince.Trim().ToUpperInvariant();
        if (string.Equals(state, "AR", StringComparison.OrdinalIgnoreCase))
        {
            var county = profile.MyLocation.Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(county) ? "AR" : county;
        }

        return string.IsNullOrWhiteSpace(state) ? "" : state;
    }

    private static (string Class, string Section) BuildFieldDaySentExchange(ConfigProfile profile, CabrilloExportSettings? settings)
    {
        var cls = ResolveHeaderValue(settings, "CATEGORY", profile.MyFieldDayClass).Trim().ToUpperInvariant();
        var section = ResolveHeaderValue(settings, "ARRL-SECTION", profile.MyFieldDaySection).Trim().ToUpperInvariant();
        return (string.IsNullOrWhiteSpace(cls) ? "" : cls, string.IsNullOrWhiteSpace(section) ? "" : section);
    }

    private static string BuildReceivedExchange(Qso qso)
    {
        var county = qso.Details?
            .FirstOrDefault(detail => string.Equals(detail.FieldName, "County", StringComparison.OrdinalIgnoreCase))
            ?.FieldValue
            ?.Trim()
            .ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(county))
            return county;

        return string.IsNullOrWhiteSpace(qso.State) ? "" : qso.State.Trim().ToUpperInvariant();
    }

    private static (string Class, string Section) BuildFieldDayReceivedExchange(Qso qso)
    {
        var cls = qso.Details?
            .FirstOrDefault(detail => string.Equals(detail.FieldName, "Class", StringComparison.OrdinalIgnoreCase))
            ?.FieldValue
            ?.Trim()
            .ToUpperInvariant() ?? string.Empty;

        var section = qso.Details?
            .FirstOrDefault(detail => string.Equals(detail.FieldName, "Section", StringComparison.OrdinalIgnoreCase))
            ?.FieldValue
            ?.Trim()
            .ToUpperInvariant() ?? string.Empty;

        return (cls, section);
    }

    private static string NormalizeCabrilloMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return "PH";

        var upper = mode.Trim().ToUpperInvariant();
        
        // CW modes
        if (upper is "CW" or "MORSE")
            return "CW";

        // Phone (voice) modes
        if (upper is "SSB" or "AM" or "FM" or "LSB" or "USB")
            return "PH";

        // Digital modes - return standardized Cabrillo code
        if (upper is "FT8" or "FT4" or "RTTY" or "RY" or "PSK" or "OLIVIA" or "HELL" or "DSTAR" or "ATV" or "JS8" or "MFSK" or "PACKET" or "THOR" or "DOMINO" or "DIGITAL")
            return "DG";

        // Default: classify as digital if contains common digital keywords
        if (upper.Contains("DIGITAL") || upper.Contains("DATA") || upper.Contains("PACKET") || upper.Contains("JT") || upper.Contains("FT"))
            return "DG";

        // Otherwise assume phone if unknown
        return "PH";
    }

    private static string NormalizeFieldDayCabrilloMode(string? mode)
    {
        return NormalizeFieldDayMode(mode) switch
        {
            "CW" => "CW",
            "DIGITAL" => "DIGITAL",
            _ => "PHONE"
        };
    }

    private static string NormalizeRst(string? rst, string cabrilloMode)
    {
        var trimmed = rst?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
            return trimmed;

        return cabrilloMode == "CW" ? "599" : "59";
    }

    private static string FormatFrequencyKhz(Qso qso)
    {
        if (qso.Freq > 0m)
        {
            var khz = (int)Math.Round(qso.Freq * 1000m, MidpointRounding.AwayFromZero);
            return khz.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(qso.Band) && TryBandToKhz(qso.Band, out var bandKhz))
            return bandKhz.ToString(CultureInfo.InvariantCulture);

        return "0";
    }

    private static bool TryBandToKhz(string band, out int khz)
    {
        khz = 0;
        var normalized = band.Trim().ToUpperInvariant();
        if (normalized.EndsWith("M", StringComparison.Ordinal))
            normalized = normalized.TrimEnd('M');

        return normalized switch
        {
            "160" => SetKhz(1800, out khz),
            "80" => SetKhz(3500, out khz),
            "60" => SetKhz(5330, out khz),
            "40" => SetKhz(7000, out khz),
            "30" => SetKhz(10100, out khz),
            "20" => SetKhz(14000, out khz),
            "17" => SetKhz(18068, out khz),
            "15" => SetKhz(21000, out khz),
            "12" => SetKhz(24890, out khz),
            "10" => SetKhz(28000, out khz),
            "6" => SetKhz(50000, out khz),
            "2" => SetKhz(144000, out khz),
            "70CM" => SetKhz(432000, out khz),
            _ => false
        };
    }

    private static bool SetKhz(int value, out int khz)
    {
        khz = value;
        return true;
    }

    private static string ResolveHeaderValue(CabrilloExportSettings? settings, string key, string? fallback)
    {
        var value = settings?.GetHeaderValue(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static string NormalizeHeader(string? value)
    {
        var resolved = value ?? string.Empty;
        return resolved.Trim().ToUpperInvariant();
    }

    private static int CalculateFieldDayScore(IReadOnlyList<Qso> qsos, int bonusPoints = 0)
    {
        // In ARRL FD: CW and digital modes count as 2 points, phone counts as 1 point
        var cwCount = 0;
        var phoneCount = 0;
        var digitalCount = 0;

        foreach (var qso in qsos)
        {
            var mode = NormalizeFieldDayMode(qso.Mode);
            if (mode == "CW")
                cwCount++;
            else if (mode == "DIGITAL")
                digitalCount++;
            else
                phoneCount++;
        }

        var qsoScore = (cwCount * 2) + (digitalCount * 2) + (phoneCount * 1);
        return qsoScore + bonusPoints;
    }

    private static string NormalizeFieldDayMode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "PHONE";

        var mode = raw.Trim().ToUpperInvariant();

        // CW modes
        if (mode is "CW" or "MORSE")
            return "CW";

        // Phone (voice) modes
        if (mode is "SSB" or "AM" or "FM" or "LSB" or "USB")
            return "PHONE";

        // Digital modes - comprehensive list matching available modes
        if (mode is "FT8" or "FT4" or "RTTY" or "RY" or "PSK" or "PSK31" or "PSK63" or "OLIVIA" or "HELL" or "DSTAR" or "ATV" or "JS8" or "MFSK" or "PACKET" or "THOR" or "DOMINO" or "DIGITAL")
            return "DIGITAL";

        // Default: classify as digital if contains common digital keywords
        if (mode.Contains("DIGITAL") || mode.Contains("DATA") || mode.Contains("PACKET") || mode.Contains("JT") || mode.Contains("FT"))
            return "DIGITAL";

        // Otherwise assume phone if unknown
        return "PHONE";
    }

    private static void AppendSoapbox(StringBuilder sb, string? soapbox)
    {
        if (string.IsNullOrWhiteSpace(soapbox))
            return;

        foreach (var line in soapbox.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            AppendHeader(sb, "SOAPBOX", line.Trim());
    }

    private static List<string> NormalizeContestIds(CabrilloContestDefinition contest)
    {
        var ids = contest.AdifContestIds.Count > 0
            ? contest.AdifContestIds
            : [contest.AdifContestId];

        return ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static DateTime? ResolveContestStart(CabrilloExportSettings? settings)
        => ParseContestDateTime(settings?.GetHeaderValue("CONTEST-START"));

    private static DateTime? ResolveContestEnd(CabrilloExportSettings? settings)
        => ParseContestDateTime(settings?.GetHeaderValue("CONTEST-END"));

    private static DateTime? ParseContestDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd HHmm", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return parsed;

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            return parsed;

        return null;
    }
}
