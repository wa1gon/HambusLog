namespace HamBusLog.ViewModels;

using Microsoft.EntityFrameworkCore;

public sealed class ArrlFdProgressViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> KnownSectionCodes = ArrlFieldDaySectionCatalog.GetAll();

    private string _sectionSummary = "Sections: 0/0";
    private string _dxSummary = "DX: Missing";
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

    public string DxSummary
    {
        get => _dxSummary;
        private set => SetProperty(ref _dxSummary, value);
    }

    public bool HasSections => SectionRows.Count > 0;
    public bool HasNoSections => !HasSections;

    public void Refresh()
    {
        var snapshot = LoadSnapshot();
        UpdateRows(SectionRows, snapshot.SectionRows);

        SectionSummary = $"Sections: {snapshot.WorkedKnownSectionCount}/{snapshot.TotalKnownSectionCount}";
        DxSummary = snapshot.DxWorked ? "DX: Worked" : "DX: Missing";
        LastUpdated = $"Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        OnPropertyChanged(nameof(HasSections));
        OnPropertyChanged(nameof(HasNoSections));
    }

    private static ArrlFdProgressSnapshot LoadSnapshot()
    {
        var contestIds = ResolveArrlFdContestIds();
        var sectionFieldNames = ResolveSectionFieldNames();
        var classFieldNames = ResolveClassFieldNames();

        var qsoIdsFromContest = App.DbContext.Qsos
            .AsNoTracking()
            .Where(q => contestIds.Contains(q.ContestId.Trim()))
            .Select(q => q.Id)
            .ToList();

        var qsoIdsFromDetails = App.DbContext.QsoDetails
            .AsNoTracking()
            .Where(d => sectionFieldNames.Contains(d.FieldName.Trim()) || classFieldNames.Contains(d.FieldName.Trim()))
            .Select(d => d.QsoId)
            .Distinct()
            .ToList();

        var fdQsoIds = qsoIdsFromContest
            .Concat(qsoIdsFromDetails)
            .Distinct()
            .ToList();

        var qsoSnapshots = App.DbContext.Qsos
            .AsNoTracking()
            .Where(q => fdQsoIds.Contains(q.Id))
            .Select(q => new FdQsoSnapshot(
                q.Id,
                q.State.Trim(),
                q.Country.Trim()))
            .ToList();

        var workedSections = App.DbContext.QsoDetails
            .AsNoTracking()
            .Where(d => fdQsoIds.Contains(d.QsoId))
            .Select(d => new { d.FieldName, d.FieldValue })
            .AsEnumerable()
            .Where(d => sectionFieldNames.Contains(d.FieldName.Trim()))
            .Select(d => NormalizeSectionCode(d.FieldValue))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Some FD imports encode DX via country/state without a section detail row.
        if (qsoSnapshots.Any(IsDxSectionCandidate))
            workedSections.Add("DX");

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

        return new ArrlFdProgressSnapshot(rows, workedKnownCount, KnownSectionCodes.Count, workedSections.Contains("DX"));
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
        int TotalKnownSectionCount,
        bool DxWorked);

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
            var exchangeType = contest.ExchangeType.Trim();

            if (IsArrlFdContestKey(key)
                || IsArrlFdContestKey(adif)
                || IsArrlFdContestName(name)
                || string.Equals(exchangeType, "fieldday", StringComparison.OrdinalIgnoreCase))
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
            "ARRL-SECTION",
            "ARRL_SECT",
            "ARRLSECT"
        };

        var config = AppConfigurationStore.Load();
        foreach (var contest in config.Contests)
        {
            var isFieldDay = IsArrlFdContestKey(contest.Key.Trim())
                             || IsArrlFdContestKey(contest.AdifContestId.Trim())
                             || IsArrlFdContestName(contest.DisplayName.Trim())
                             || string.Equals(contest.ExchangeType.Trim(), "fieldday", StringComparison.OrdinalIgnoreCase);

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

    private static HashSet<string> ResolveClassFieldNames()
    {
        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Class",
            "FD_CLASS"
        };

        var config = AppConfigurationStore.Load();
        foreach (var contest in config.Contests)
        {
            var isFieldDay = IsArrlFdContestKey(contest.Key.Trim())
                             || IsArrlFdContestKey(contest.AdifContestId.Trim())
                             || IsArrlFdContestName(contest.DisplayName.Trim())
                             || string.Equals(contest.ExchangeType.Trim(), "fieldday", StringComparison.OrdinalIgnoreCase);

            if (!isFieldDay)
                continue;

            foreach (var field in contest.RequiredFields)
            {
                if (!string.Equals(field.Key.Trim(), ContestFieldKeys.FieldDayClass, StringComparison.OrdinalIgnoreCase))
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

    private static bool IsDxSectionCandidate(FdQsoSnapshot qso)
    {
        var state = qso.State.Trim().ToUpperInvariant();
        if (state == "DX")
            return true;

        var country = qso.Country.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(country))
            return false;

        return country is not ("US" or "USA" or "UNITED STATES" or "UNITED STATES OF AMERICA" or "CANADA");
    }

    private sealed record FdQsoSnapshot(Guid Id, string State, string Country);
}








