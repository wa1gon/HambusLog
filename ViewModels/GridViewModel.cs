namespace HamBusLog.ViewModels;

public enum ContestType
{
    Normal,
    ArrlFieldDay
}

public class GridViewModel
{
    public ObservableCollection<Qso> LogEntries { get; }
    private readonly ObservableCollection<Qso> _filteredEntries = [];
    public ObservableCollection<Qso> FilteredEntries { get; }
    public List<ContestType> ContestTypes { get; } = new() { ContestType.Normal, ContestType.ArrlFieldDay };
    
    private ContestType _selectedContestType = ContestType.Normal;
    private string _searchCall = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ContestType SelectedContestType
    {
        get => _selectedContestType;
        set
        {
            if (_selectedContestType != value)
            {
                _selectedContestType = value;
                OnPropertyChanged(nameof(SelectedContestType));
            }
        }
    }

    public string SearchCall
    {
        get => _searchCall;
        set
        {
            if (_searchCall == value)
                return;
            _searchCall = value;
            OnPropertyChanged(nameof(SearchCall));
            RefreshFilter();
        }
    }
    
    private readonly Dictionary<string, bool> _sortAscendingByColumn = new();
    private readonly CallValidator _callValidator = new();
    private readonly BandValidator _bandValidator = new();
    private readonly ModeValidator _modeValidator = new();
    private readonly SectionValidator _sectionValidator = new();
    private readonly ClassValidator _classValidator = new();
    
    // Common input fields
    public string InputCall { get; set; } = string.Empty;
    public string InputDate { get; set; } = string.Empty;
    public string InputBand { get; set; } = string.Empty;
    public string InputMode { get; set; } = string.Empty;
    public string InputTimeOn { get; set; } = string.Empty;
    public string InputSent { get; set; } = string.Empty;
    public string InputRec { get; set; } = string.Empty;
    public string InputFreq { get; set; } = string.Empty;
    
    // ARRL Field Day specific
    public string InputFieldDaySection { get; set; } = string.Empty;
    public string InputFieldDayClass { get; set; } = string.Empty;

    private string _callError = string.Empty;
    private string _bandError = string.Empty;
    private string _modeError = string.Empty;
    private string _sectionError = string.Empty;
    private string _classError = string.Empty;

    public string CallError
    {
        get => _callError;
        private set => SetValidationProperty(ref _callError, value, nameof(CallError), nameof(HasCallError));
    }

    public string BandError
    {
        get => _bandError;
        private set => SetValidationProperty(ref _bandError, value, nameof(BandError), nameof(HasBandError));
    }

    public string ModeError
    {
        get => _modeError;
        private set => SetValidationProperty(ref _modeError, value, nameof(ModeError), nameof(HasModeError));
    }

    public string SectionError
    {
        get => _sectionError;
        private set => SetValidationProperty(ref _sectionError, value, nameof(SectionError), nameof(HasSectionError));
    }

    public string ClassError
    {
        get => _classError;
        private set => SetValidationProperty(ref _classError, value, nameof(ClassError), nameof(HasClassError));
    }

    public bool HasCallError => !string.IsNullOrWhiteSpace(CallError);
    public bool HasBandError => !string.IsNullOrWhiteSpace(BandError);
    public bool HasModeError => !string.IsNullOrWhiteSpace(ModeError);
    public bool HasSectionError => !string.IsNullOrWhiteSpace(SectionError);
    public bool HasClassError => !string.IsNullOrWhiteSpace(ClassError);
    
    public GridViewModel()
    {
        LogEntries = new ObservableCollection<Qso>
        {
            new Qso { Call = "W5XYZ", QsoDate = Convert.ToDateTime("2026-04-21 14:30"), Freq = 7.250m, Mode = "SSB", RstRcvd = "59" },
            new Qso { Call = "K0ABC", QsoDate = Convert.ToDateTime("2026-04-21 14:45"), Freq = 14.250m, Mode = "CW", RstRcvd = "579" },
            new Qso { Call = "N1XYZ", QsoDate = Convert.ToDateTime("2026-04-21 15:00"), Freq = 21.250m, Mode = "SSB", RstRcvd = "57" },
            new Qso { Call = "VE3XYZ", QsoDate = Convert.ToDateTime("2026-04-21 15:15"), Freq = 3.650m, Mode = "LSB", RstRcvd = "549" },
            new Qso { Call = "W4ABC", QsoDate = Convert.ToDateTime("2026-04-21 15:30"), Freq = 28.400m, Mode = "FM", RstRcvd = "59+" },
            new Qso { Call = "K5XYZ", QsoDate = Convert.ToDateTime("2026-04-21 15:45"), Freq = 7.180m, Mode = "CW", RstRcvd = "589" },
            new Qso { Call = "W6ZZZ", QsoDate = Convert.ToDateTime("2026-04-21 16:00"), Freq = 14.200m, Mode = "SSB", RstRcvd = "55" },
            new Qso { Call = "N2ABC", QsoDate = Convert.ToDateTime("2026-04-21 16:15"), Freq = 3.750m, Mode = "USB", RstRcvd = "559" },
            new Qso { Call = "VE2XYZ", QsoDate = Convert.ToDateTime("2026-04-21 16:30"), Freq = 21.350m, Mode = "CW", RstRcvd = "569" },
            new Qso { Call = "W7ABC", QsoDate = Convert.ToDateTime("2026-04-21 16:45"), Freq = 10.120m, Mode = "CW", RstRcvd = "579" }
        };
        FilteredEntries = _filteredEntries;
        LogEntries.CollectionChanged += (_, _) => RefreshFilter();
        RefreshFilter();
    }
    
    public void AddNewEntry()
    {
        ClearValidationErrors();

        InputBand = (InputBand ?? string.Empty).Trim().ToUpperInvariant();
        InputMode = (InputMode ?? string.Empty).Trim().ToUpperInvariant();
        InputFieldDaySection = (InputFieldDaySection ?? string.Empty).Trim().ToUpperInvariant();
        InputFieldDayClass = (InputFieldDayClass ?? string.Empty).Trim().ToUpperInvariant();

        var callResult = _callValidator.Validate(InputCall);
        if (!callResult.IsValid)
        {
            CallError = callResult.ErrorMessage;
            return;
        }

        var bandResult = _bandValidator.Validate(InputBand);
        if (!bandResult.IsValid)
        {
            BandError = bandResult.ErrorMessage;
            return;
        }

        var modeResult = _modeValidator.Validate(InputMode);
        if (!modeResult.IsValid)
        {
            ModeError = modeResult.ErrorMessage;
            return;
        }

        if (SelectedContestType == ContestType.ArrlFieldDay)
        {
            var sectionResult = _sectionValidator.Validate(InputFieldDaySection);
            if (!sectionResult.IsValid)
            {
                SectionError = sectionResult.ErrorMessage;
                return;
            }

            var classResult = _classValidator.Validate(InputFieldDayClass);
            if (!classResult.IsValid)
            {
                ClassError = classResult.ErrorMessage;
                return;
            }
        }

        var qsoDate = DateTime.TryParse(InputDate, out var parsedDate)
            ? parsedDate
            : DateTime.Now;
        var freq = decimal.TryParse(InputFreq, out var parsedFreq)
            ? parsedFreq
            : 0m;
        
        var newEntry = new Qso
        {
            Call = InputCall,
            QsoDate = qsoDate,
            Band = InputBand,
            Mode = InputMode,
            RstSent = SelectedContestType == ContestType.ArrlFieldDay ? string.Empty : InputSent,
            RstRcvd = SelectedContestType == ContestType.ArrlFieldDay ? string.Empty : InputRec,
            Freq = freq
        };

        newEntry.QslInfo ??= new List<QsoQslInfo>();
        newEntry.Details ??= new List<QsoDetail>();
        
        // Store contest-specific fields in QsoDetails
        if (SelectedContestType == ContestType.ArrlFieldDay && 
            (!string.IsNullOrWhiteSpace(InputFieldDaySection) || !string.IsNullOrWhiteSpace(InputFieldDayClass)))
        {
            if (!string.IsNullOrWhiteSpace(InputFieldDaySection))
                newEntry.Details.Add(new QsoDetail { FieldName = "Section", FieldValue = InputFieldDaySection });
            if (!string.IsNullOrWhiteSpace(InputFieldDayClass))
                newEntry.Details.Add(new QsoDetail { FieldName = "Class", FieldValue = InputFieldDayClass });
        }
        
        LogEntries.Add(newEntry);
        RefreshFilter();
        ClearInputs();
    }

    public void SortBy(string column)
    {
        if (string.IsNullOrWhiteSpace(column) || LogEntries.Count == 0)
            return;

        var ascending = !_sortAscendingByColumn.GetValueOrDefault(column, false);
        _sortAscendingByColumn[column] = ascending;

        IEnumerable<Qso> sorted = column switch
        {
            "Call" => ascending
                ? LogEntries.OrderBy(x => x.Call)
                : LogEntries.OrderByDescending(x => x.Call),
            "QsoDate" => ascending
                ? LogEntries.OrderBy(x => x.QsoDate)
                : LogEntries.OrderByDescending(x => x.QsoDate),
            "Freq" => ascending
                ? LogEntries.OrderBy(x => x.Freq)
                : LogEntries.OrderByDescending(x => x.Freq),
            "Mode" => ascending
                ? LogEntries.OrderBy(x => x.Mode)
                : LogEntries.OrderByDescending(x => x.Mode),
            "RstRcvd" => ascending
                ? LogEntries.OrderBy(x => x.RstRcvd)
                : LogEntries.OrderByDescending(x => x.RstRcvd),
            _ => LogEntries
        };

        var snapshot = sorted.ToList();
        LogEntries.Clear();
        foreach (var item in snapshot)
            LogEntries.Add(item);
    }
    
    private void ClearInputs()
    {
        InputCall = string.Empty;
        InputDate = string.Empty;
        InputBand = string.Empty;
        InputMode = string.Empty;
        InputTimeOn = string.Empty;
        InputSent = string.Empty;
        InputRec = string.Empty;
        InputFreq = string.Empty;
        InputFieldDaySection = string.Empty;
        InputFieldDayClass = string.Empty;
    }

    private void ClearValidationErrors()
    {
        CallError = string.Empty;
        BandError = string.Empty;
        ModeError = string.Empty;
        SectionError = string.Empty;
        ClassError = string.Empty;
    }

    private void SetValidationProperty(ref string field, string value, string propertyName, string visibilityPropertyName)
    {
        if (field == value)
            return;

        field = value;
        OnPropertyChanged(propertyName);
        OnPropertyChanged(visibilityPropertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RefreshFilter()
    {
        var term = _searchCall.Trim();
        _filteredEntries.Clear();
        foreach (var q in LogEntries)
        {
            if (string.IsNullOrWhiteSpace(term) ||
                q.Call.Contains(term, StringComparison.OrdinalIgnoreCase))
                _filteredEntries.Add(q);
        }
    }
}

public class LogEntry
{
    public string Call { get; set; } = string.Empty;
    public string DateTime { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public decimal Freq { get; set; } = 0.0m;
    public string Mode { get; set; } = string.Empty;
    public string RstRcvd { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}
