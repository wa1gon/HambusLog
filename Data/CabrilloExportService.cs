using System.Text;

namespace HamBusLog.Data;

public static class CabrilloExportService
{
    private const string ArqpContestId = "AR-QSO-PARTY";
    private const string ArrlFieldDayContestId = "ARRL-FIELD-DAY";

    public static async Task<int> ExportArqpToFileAsync(
        string filePath,
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

        var sinceUtc = DateTime.UtcNow.AddMonths(-6);

        await using var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, connectionString);
        var qsos = await db.Qsos
            .Include(x => x.Details)
            .Where(x => x.QsoDate >= sinceUtc
                        && x.ContestId != null
                        && (x.ContestId.ToUpper() == ArqpContestId || x.ContestId.ToUpper() == "ARQP"))
            .OrderBy(x => x.QsoDate)
            .ThenBy(x => x.Call)
            .ToListAsync(cancellationToken);

        var cabrillo = BuildArqpCabrillo(profile, settings, qsos);
        await File.WriteAllTextAsync(fullPath, cabrillo, cancellationToken);
        return qsos.Count;
    }

    public static async Task<int> ExportArrlFieldDayToFileAsync(
        string filePath,
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

        var sinceUtc = DateTime.UtcNow.AddMonths(-6);

        await using var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, connectionString);
        var qsos = await db.Qsos
            .Include(x => x.Details)
            .Where(x => x.QsoDate >= sinceUtc
                        && x.ContestId != null
                        && (x.ContestId.ToUpper() == ArrlFieldDayContestId || x.ContestId.ToUpper() == "ARRL-FD"))
            .OrderBy(x => x.QsoDate)
            .ThenBy(x => x.Call)
            .ToListAsync(cancellationToken);

        var cabrillo = BuildArrlFieldDayCabrillo(profile, settings, qsos);
        await File.WriteAllTextAsync(fullPath, cabrillo, cancellationToken);
        return qsos.Count;
    }

    private static string BuildArqpCabrillo(ConfigProfile profile, CabrilloExportSettings? settings, IReadOnlyList<Qso> qsos)
    {
        var sb = new StringBuilder();
        var callSign = ResolveHeaderValue(settings?.CallSign, profile.StationCallSign).ToUpperInvariant();
        var location = ResolveHeaderValue(settings?.Location, profile.MyStateProvince).ToUpperInvariant();

        AppendHeader(sb, "START-OF-LOG", "3.1");
        AppendHeader(sb, "CREATED-BY", "HamBusLog");
        AppendHeader(sb, "CONTEST", ArqpContestId);
        AppendHeader(sb, "CALLSIGN", callSign);
        AppendHeader(sb, "CATEGORY-OPERATOR", NormalizeHeader(settings?.CategoryOperator, "SINGLE-OP"));
        AppendHeader(sb, "CATEGORY-ASSISTED", NormalizeHeader(settings?.CategoryAssisted, "NON-ASSISTED"));
        AppendHeader(sb, "CATEGORY-BAND", NormalizeHeader(settings?.CategoryBand, "ALL"));
        AppendHeader(sb, "CATEGORY-MODE", NormalizeHeader(settings?.CategoryMode, "MIXED"));
        AppendHeader(sb, "CATEGORY-POWER", NormalizeHeader(settings?.CategoryPower, "LOW"));
        AppendHeader(sb, "CATEGORY-TRANSMITTER", NormalizeHeader(settings?.CategoryTransmitter, "ONE"));
        AppendHeader(sb, "LOCATION", location);
        AppendHeader(sb, "CLAIMED-SCORE", ResolveClaimedScore(settings?.ClaimedScore, qsos.Count));
        AppendHeader(sb, "OPERATORS", ResolveHeaderValue(settings?.Operators, callSign));
        AppendHeader(sb, "CLUB", settings?.Club);
        AppendHeader(sb, "ADDRESS", settings?.Address);
        AppendHeader(sb, "EMAIL", settings?.Email);

        if (!string.IsNullOrWhiteSpace(settings?.Soapbox))
        {
            foreach (var line in settings.Soapbox.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                AppendHeader(sb, "SOAPBOX", line.Trim());
        }

        foreach (var qso in qsos)
            sb.AppendLine(FormatArqpQsoLine(profile, qso));

        sb.AppendLine("END-OF-LOG:");
        return sb.ToString();
    }

    private static string BuildArrlFieldDayCabrillo(ConfigProfile profile, CabrilloExportSettings? settings, IReadOnlyList<Qso> qsos)
    {
        var sb = new StringBuilder();
        var callSign = ResolveHeaderValue(settings?.CallSign, profile.StationCallSign).ToUpperInvariant();
        var location = ResolveHeaderValue(settings?.Location, profile.MyStateProvince).ToUpperInvariant();
        var category = ResolveHeaderValue(settings?.Category, profile.MyFieldDayClass).ToUpperInvariant();
        var arrlSection = ResolveHeaderValue(settings?.ArrlSection, profile.MyFieldDaySection).ToUpperInvariant();

        AppendHeader(sb, "START-OF-LOG", "3.1");
        AppendHeader(sb, "CREATED-BY", "HamBusLog");
        AppendHeader(sb, "CONTEST", ArrlFieldDayContestId);
        AppendHeader(sb, "CALLSIGN", callSign);
        AppendHeader(sb, "CATEGORY", category);
        AppendHeader(sb, "LOCATION", location);
        AppendHeader(sb, "ARRL-SECTION", arrlSection);
        AppendHeader(sb, "CLAIMED-SCORE", ResolveClaimedScore(settings?.ClaimedScore, qsos.Count));
        AppendHeader(sb, "OPERATORS", ResolveHeaderValue(settings?.Operators, callSign));
        AppendHeader(sb, "NAME", settings?.Name);
        AppendHeader(sb, "ADDRESS", settings?.Address);
        AppendHeader(sb, "ADDRESS-CITY", settings?.AddressCity);
        AppendHeader(sb, "ADDRESS-STATE-PROVINCE", settings?.AddressStateProvince);
        AppendHeader(sb, "ADDRESS-POSTALCODE", settings?.AddressPostalCode);
        AppendHeader(sb, "ADDRESS-COUNTRY", settings?.AddressCountry);
        AppendHeader(sb, "CLUB", settings?.Club);
        AppendHeader(sb, "EMAIL", settings?.Email);

        if (!string.IsNullOrWhiteSpace(settings?.Soapbox))
        {
            foreach (var line in settings.Soapbox.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                AppendHeader(sb, "SOAPBOX", line.Trim());
        }

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
        var mode = NormalizeCabrilloMode(qso.Mode);
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
        var mode = NormalizeCabrilloMode(qso.Mode);
        var date = utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var time = utc.ToString("HHmm", CultureInfo.InvariantCulture);
        var myCall = ResolveHeaderValue(settings?.CallSign, profile.StationCallSign).ToUpperInvariant();
        var (myClass, mySection) = BuildFieldDaySentExchange(profile, settings);
        var (theirClass, theirSection) = BuildFieldDayReceivedExchange(qso);
        var hisCall = (qso.Call ?? string.Empty).Trim().ToUpperInvariant();

        return string.Format(CultureInfo.InvariantCulture,
            "QSO: {0,6} {1,-2} {2} {3} {4,-13} {5,-2} {6,-6} {7,-13} {8,-2} {9,-6}",
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
        var cls = ResolveHeaderValue(settings?.Category, profile.MyFieldDayClass).Trim().ToUpperInvariant();
        var section = ResolveHeaderValue(settings?.ArrlSection, profile.MyFieldDaySection).Trim().ToUpperInvariant();
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
        if (upper.Contains("CW", StringComparison.OrdinalIgnoreCase))
            return "CW";
        if (upper.Contains("FT", StringComparison.OrdinalIgnoreCase)
            || upper.Contains("RTTY", StringComparison.OrdinalIgnoreCase)
            || upper.Contains("PSK", StringComparison.OrdinalIgnoreCase)
            || upper.Contains("DIG", StringComparison.OrdinalIgnoreCase))
            return "DG";

        return "PH";
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

    private static string ResolveHeaderValue(string? value, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static string NormalizeHeader(string? value, string fallback)
    {
        var resolved = ResolveHeaderValue(value, fallback);
        return resolved.ToUpperInvariant();
    }

    private static string ResolveClaimedScore(string? value, int fallback)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback.ToString(CultureInfo.InvariantCulture) : trimmed;
    }
}
