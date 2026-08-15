namespace HamBusLog.ViewModels;

public enum WasConfirmationSource
{
    LotwOnly,
    AnyQsl,
    Worked
}

public sealed class WasConfirmationSourceOption
{
    public WasConfirmationSourceOption(WasConfirmationSource value, string display)
    {
        Value = value;
        Display = display;
    }

    public WasConfirmationSource Value { get; }
    public string Display { get; }

    public override string ToString() => Display;
}

public sealed class WasProgressViewModel : ViewModelBase
{
    // WAS covers the 50 U.S. states (DC does not count toward WAS).
    private static readonly IReadOnlyList<string> StateCodes =
    [
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
    ];

    private WasConfirmationSourceOption _selectedConfirmationSource;
    private string _stateSummary = $"States: 0/{StateCodes.Count}";
    private string _lastUpdated = "Last updated: never";

    public WasProgressViewModel()
    {
        _selectedConfirmationSource = ConfirmationSources[0];
    }

    public ObservableCollection<WasConfirmationSourceOption> ConfirmationSources { get; } =
    [
        new WasConfirmationSourceOption(WasConfirmationSource.LotwOnly, "LoTW confirmed"),
        new WasConfirmationSourceOption(WasConfirmationSource.AnyQsl, "Any QSL confirmed"),
        new WasConfirmationSourceOption(WasConfirmationSource.Worked, "Worked (no confirmation)")
    ];

    public ObservableCollection<ProgressRow> StateRows { get; } = [];

    public WasConfirmationSourceOption SelectedConfirmationSource
    {
        get => _selectedConfirmationSource;
        set
        {
            if (SetProperty(ref _selectedConfirmationSource, value))
                Refresh();
        }
    }

    public string StateSummary
    {
        get => _stateSummary;
        private set => SetProperty(ref _stateSummary, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        private set => SetProperty(ref _lastUpdated, value);
    }

    public bool HasStates => StateRows.Count > 0;
    public bool HasNoStates => !HasStates;

    public void Refresh()
    {
        var snapshot = LoadSnapshot(SelectedConfirmationSource.Value);
        UpdateRows(StateRows, snapshot.StateRows);

        StateSummary = $"States: {snapshot.WorkedStateCount}/{snapshot.TotalStateCount}";
        LastUpdated = $"Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        OnPropertyChanged(nameof(HasStates));
        OnPropertyChanged(nameof(HasNoStates));
    }

    private static WasProgressSnapshot LoadSnapshot(WasConfirmationSource source)
    {
        List<Guid>? confirmedQsoIds = source switch
        {
            WasConfirmationSource.LotwOnly => App.DbContext.QsoQslInfos
                .AsNoTracking()
                .Where(x => x.QslReceived && x.QslService == "LOTW")
                .Select(x => x.QsoId)
                .Distinct()
                .ToList(),
            WasConfirmationSource.AnyQsl => App.DbContext.QsoQslInfos
                .AsNoTracking()
                .Where(x => x.QslReceived)
                .Select(x => x.QsoId)
                .Distinct()
                .ToList(),
            _ => null
        };

        var statesQuery = App.DbContext.Qsos.AsNoTracking();
        var states = (confirmedQsoIds is null
                ? statesQuery
                : statesQuery.Where(q => confirmedQsoIds.Contains(q.Id)))
            .Select(q => q.State.Trim().ToUpperInvariant())
            .ToList();

        var workedStates = states
            .Where(code => code.Length == 2 && StateCodes.Contains(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stateRows = StateCodes
            .Select(code => new ProgressRow(code, workedStates.Contains(code)))
            .ToList();

        return new WasProgressSnapshot(stateRows, workedStates.Count, StateCodes.Count);
    }

    private static void UpdateRows(ObservableCollection<ProgressRow> target, IReadOnlyList<ProgressRow> rows)
    {
        target.Clear();
        foreach (var row in rows)
            target.Add(row);
    }

    private sealed record WasProgressSnapshot(IReadOnlyList<ProgressRow> StateRows, int WorkedStateCount, int TotalStateCount);
}
