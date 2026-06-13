namespace HamBusLog.ViewModels;

using Microsoft.EntityFrameworkCore;

public sealed class ArrlFdProgressViewModel : ViewModelBase
{
    // Includes common ARRL and RAC section identifiers used by Field Day logs.
    private static readonly IReadOnlyList<string> KnownSectionCodes =
    [
        "AB", "AK", "AL", "AR", "AZ", "BC", "CO", "CT", "DC", "DE", "EB", "EMA", "ENY", "EPA", "EWA",
        "GA", "GH", "IA", "ID", "IL", "IN", "KS", "KY", "LA", "LAX", "MAR", "MB", "MDC", "ME", "MI",
        "MN", "MO", "MS", "MT", "NB", "NC", "ND", "NE", "NFL", "NH", "NL", "NLI", "NM", "NNJ", "NNY",
        "NS", "NT", "NTX", "NV", "NY", "OH", "OK", "ON", "ONE", "ONN", "ONS", "OR", "ORG", "PAC", "PE",
        "PR", "QC", "RI", "SB", "SC", "SCV", "SD", "SDG", "SF", "SFL", "SJV", "SK", "SNJ", "STX", "SV",
        "TER", "TN", "UT", "VA", "VI", "VT", "WCF", "WI", "WMA", "WNY", "WPA", "WTX", "WV", "WWA", "WY"
    ];

    private string _sectionSummary = "Sections: 0/0";
    private string _lastUpdated = "Last updated: never";

    public ObservableCollection<ProgressRow> SectionRows { get; } = [];

    public string SectionSummary
    {
        get => _sectionSummary;
        private set => SetProperty(ref _sectionSummary, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        private set => SetProperty(ref _lastUpdated, value);
    }

    public bool HasSections => SectionRows.Count > 0;
    public bool HasNoSections => !HasSections;

    public void Refresh()
    {
        var snapshot = LoadSnapshot();
        UpdateRows(SectionRows, snapshot.SectionRows);

        SectionSummary = $"Sections: {snapshot.WorkedKnownSectionCount}/{snapshot.TotalKnownSectionCount}";
        LastUpdated = $"Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        OnPropertyChanged(nameof(HasSections));
        OnPropertyChanged(nameof(HasNoSections));
    }

    private static ArrlFdProgressSnapshot LoadSnapshot()
    {
        var contestIds = ResolveArrlFdContestIds();
        var sectionFieldNames = ResolveSectionFieldNames();

        var qsoIds = App.DbContext.Qsos
            .AsNoTracking()
            .Where(q => contestIds.Contains(q.ContestId.Trim()))
            .Select(q => q.Id)
            .ToList();

        var workedSections = App.DbContext.QsoDetails
            .AsNoTracking()
            .Where(d => qsoIds.Contains(d.QsoId))
            .Select(d => new { d.FieldName, d.FieldValue })
            .AsEnumerable()
            .Where(d => sectionFieldNames.Contains(d.FieldName.Trim()))
            .Select(d => NormalizeSectionCode(d.FieldValue))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var knownSet = KnownSectionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var knownRows = KnownSectionCodes
            .Select(code => new ProgressRow(code, workedSections.Contains(code)))
            .ToList();

        var extraRows = workedSections
            .Where(code => !knownSet.Contains(code))
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Select(code => new ProgressRow(code, true))
            .ToList();

        var rows = knownRows.Concat(extraRows).ToList();
        var workedKnownCount = knownRows.Count(x => x.IsWorked);

        return new ArrlFdProgressSnapshot(rows, workedKnownCount, KnownSectionCodes.Count);
    }

    private static string NormalizeSectionCode(string? raw)
    {
        return (raw ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static void UpdateRows(ObservableCollection<ProgressRow> target, IReadOnlyList<ProgressRow> rows)
    {
        target.Clear();
        foreach (var row in rows)
            target.Add(row);
    }

    private sealed record ArrlFdProgressSnapshot(
        IReadOnlyList<ProgressRow> SectionRows,
        int WorkedKnownSectionCount,
        int TotalKnownSectionCount);

    private static HashSet<string> ResolveArrlFdContestIds()
    {
        var contestIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ARRL-FD",
            "ARRL-FIELD-DAY"
        };

        var config = AppConfigurationStore.Load();
        foreach (var contest in config.Contests)
        {
            var key = contest.Key.Trim();
            var adif = contest.AdifContestId.Trim();
            var name = contest.DisplayName.Trim();

            if (IsArrlFdContestKey(key) || IsArrlFdContestKey(adif) || IsArrlFdContestName(name))
            {
                if (!string.IsNullOrWhiteSpace(key))
                    contestIds.Add(key);
                if (!string.IsNullOrWhiteSpace(adif))
                    contestIds.Add(adif);
            }
        }

        return contestIds;
    }

    private static HashSet<string> ResolveSectionFieldNames()
    {
        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Section",
            "ARRL_SECT",
            "ARRLSECT"
        };

        var config = AppConfigurationStore.Load();
        foreach (var contest in config.Contests)
        {
            var isFieldDay = IsArrlFdContestKey(contest.Key.Trim())
                             || IsArrlFdContestKey(contest.AdifContestId.Trim())
                             || IsArrlFdContestName(contest.DisplayName.Trim());

            if (!isFieldDay)
                continue;

            foreach (var field in contest.RequiredFields)
            {
                if (!string.Equals(field.Key.Trim(), ContestFieldKeys.FieldDaySection, StringComparison.OrdinalIgnoreCase))
                    continue;

                var detailFieldName = field.DetailFieldName.Trim();
                if (!string.IsNullOrWhiteSpace(detailFieldName))
                    fieldNames.Add(detailFieldName);
            }
        }

        return fieldNames;
    }

    private static bool IsArrlFdContestKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var upper = value.Trim().ToUpperInvariant();
        return upper is "ARRL-FD" or "ARRL-FIELD-DAY";
    }

    private static bool IsArrlFdContestName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains("ARRL Field Day", StringComparison.OrdinalIgnoreCase)
               || value.Contains("Field Day", StringComparison.OrdinalIgnoreCase);
    }
}


