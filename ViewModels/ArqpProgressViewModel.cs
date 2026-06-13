namespace HamBusLog.ViewModels;

using Microsoft.EntityFrameworkCore;

public sealed class ArqpProgressViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> StateCodes =
    [
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
        "DC"
    ];

    private static readonly IReadOnlyList<string> CountyCodes =
    [
        "ARK", "ASH", "BAX", "BEN", "BOO", "BRA", "CAL", "CAR", "CHI", "CLA",
        "CLY", "CLE", "CLV", "COL", "CON", "CRA", "CRW", "CRI", "CRO", "DAL",
        "DES", "DRE", "FAU", "FRA", "FUL", "GAR", "GRA", "GRE", "HEM", "HSP",
        "HOW", "IND", "IZA", "JAC", "JEF", "JOH", "LAF", "LAW", "LEE", "LIN",
        "LIR", "LOG", "LON", "MAD", "MAR", "MIL", "MIS", "MON", "MNT", "NEV",
        "NEW", "OUA", "PER", "PHI", "PIK", "POI", "POL", "POP", "PRA", "PUL",
        "RAN", "STF", "SAL", "SCO", "SEA", "SEB", "SEV", "SHA", "STO", "UNI",
        "VAN", "WAS", "WHI", "WOO", "YEL"
    ];

    private string _stateSummary = "States: 0/0";
    private string _countySummary = "Counties: 0/0";
    private string _lastUpdated = "Last updated: never";

    public ObservableCollection<ProgressRow> StateRows { get; } = [];
    public ObservableCollection<ProgressRow> CountyRows { get; } = [];

    public string StateSummary
    {
        get => _stateSummary;
        private set => SetProperty(ref _stateSummary, value);
    }

    public string CountySummary
    {
        get => _countySummary;
        private set => SetProperty(ref _countySummary, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        private set => SetProperty(ref _lastUpdated, value);
    }

    public bool HasStates => StateRows.Count > 0;
    public bool HasNoStates => !HasStates;
    public bool HasCounties => CountyRows.Count > 0;
    public bool HasNoCounties => !HasCounties;

    public void Refresh()
    {
        var snapshot = LoadSnapshot();
        UpdateRows(StateRows, snapshot.StateRows);
        UpdateRows(CountyRows, snapshot.CountyRows);

        StateSummary = $"States: {snapshot.WorkedStateCount}/{snapshot.TotalStateCount}";
        CountySummary = $"Counties: {snapshot.WorkedCountyCount}/{snapshot.TotalCountyCount}";
        LastUpdated = $"Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        OnPropertyChanged(nameof(HasStates));
        OnPropertyChanged(nameof(HasNoStates));
        OnPropertyChanged(nameof(HasCounties));
        OnPropertyChanged(nameof(HasNoCounties));
    }

    private static ArqpProgressSnapshot LoadSnapshot()
    {
        var contestIds = ResolveArqpContestIds();

        var qsoRows = App.DbContext.Qsos
            .AsNoTracking()
            .Where(q => contestIds.Contains(q.ContestId.Trim()))
            .Select(q => new QsoSnapshot(
                q.Id,
                q.State.Trim().ToUpperInvariant()))
            .ToList();

        var workedStates = qsoRows
            .Select(q => q.State)
            .Where(code => code.Length == 2 && StateCodes.Contains(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var qsoIds = qsoRows.Select(q => q.Id).ToList();
        var workedCounties = App.DbContext.QsoDetails
            .AsNoTracking()
            .Where(d => qsoIds.Contains(d.QsoId))
            .Select(d => new { d.FieldName, d.FieldValue })
            .AsEnumerable()
            .Where(d => string.Equals(d.FieldName, "County", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.FieldValue.Trim().ToUpperInvariant())
            .Where(code => code.Length == 3 && code.All(char.IsLetter))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stateRows = StateCodes
            .Select(code => new ProgressRow(code, workedStates.Contains(code)))
            .ToList();

        var countyRows = CountyCodes
            .Select(code => new ProgressRow(code, workedCounties.Contains(code)))
            .ToList();

        return new ArqpProgressSnapshot(
            stateRows,
            countyRows,
            workedStates.Count,
            StateCodes.Count,
            workedCounties.Count,
            CountyCodes.Count);
    }

    private static void UpdateRows(ObservableCollection<ProgressRow> target, IReadOnlyList<ProgressRow> rows)
    {
        target.Clear();
        foreach (var row in rows)
            target.Add(row);
    }

    private sealed record ArqpProgressSnapshot(
        IReadOnlyList<ProgressRow> StateRows,
        IReadOnlyList<ProgressRow> CountyRows,
        int WorkedStateCount,
        int TotalStateCount,
        int WorkedCountyCount,
        int TotalCountyCount);

    private static HashSet<string> ResolveArqpContestIds()
    {
        var contestIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ARQP",
            "AR-QSO-PARTY"
        };

        var config = AppConfigurationStore.Load();
        foreach (var contest in config.Contests)
        {
            var key = contest.Key.Trim();
            var adif = contest.AdifContestId.Trim();
            var name = contest.DisplayName.Trim();

            if (IsArqpContestKey(key) || IsArqpContestKey(adif) || IsArqpContestName(name))
            {
                if (!string.IsNullOrWhiteSpace(key))
                    contestIds.Add(key);
                if (!string.IsNullOrWhiteSpace(adif))
                    contestIds.Add(adif);
            }
        }

        return contestIds;
    }

    private static bool IsArqpContestKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var upper = value.Trim().ToUpperInvariant();
        return upper == "ARQP" || upper == "AR-QSO-PARTY";
    }

    private static bool IsArqpContestName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains("Arkansas QSO Party", StringComparison.OrdinalIgnoreCase)
               || value.Contains("ARQP", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record QsoSnapshot(Guid Id, string State);
}

public sealed class ProgressRow
{
    public ProgressRow(string code, bool isWorked)
    {
        Code = code;
        IsWorked = isWorked;
        if (isWorked)
        {
            BackgroundBrush = new SolidColorBrush(Color.Parse("#1E3A2F"));
            ForegroundBrush = ResolveBrush("AppForegroundBrush", Brushes.White);
            Opacity = 1.0;
            FontWeight = FontWeight.SemiBold;
        }
        else
        {
            BackgroundBrush = Brushes.Transparent;
            ForegroundBrush = ResolveBrush("AppMutedForegroundBrush", Brushes.Gray);
            Opacity = 0.45;
            FontWeight = FontWeight.Normal;
        }
    }

    public string Code { get; }
    public bool IsWorked { get; }
    public string Status => IsWorked ? "Worked" : "Missing";
    public IBrush BackgroundBrush { get; }
    public IBrush ForegroundBrush { get; }
    public double Opacity { get; }
    public FontWeight FontWeight { get; }

    private static IBrush ResolveBrush(string key, IBrush fallback)
    {
        if (Application.Current?.Resources is ResourceDictionary resources
            && resources.TryGetValue(key, out var value)
            && value is IBrush brush)
            return brush;

        return fallback;
    }
}







