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
    private readonly IQsoRepository _repository;
    private bool _isLoading = true;
    private string _loadingMessage = "Loading QSO records...";

    private bool _isBatchUpdating;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }
    }
    
    public string LoadingMessage
    {
        get => _loadingMessage;
        private set
        {
            if (_loadingMessage != value)
            {
                _loadingMessage = value;
                OnPropertyChanged(nameof(LoadingMessage));
            }
        }
    }
    
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
    
    public GridViewModel(IQsoRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        LogEntries = new ObservableCollection<Qso>();
        FilteredEntries = _filteredEntries;
        LogEntries.CollectionChanged += (_, _) =>
        {
            if (_isBatchUpdating)
                return;
            RefreshFilter();
        };
        
        // Load data synchronously to ensure it's available when the ViewModel is constructed
        LoadDataSync();
    }

    private void LoadDataSync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Starting to load QSOs from database (synchronous)...");
            
            // Load data synchronously
            var task = _repository.GetAllAsync();
            task.Wait();
            var qsos = task.Result;
            
            System.Diagnostics.Debug.WriteLine($"Database returned {qsos.Count} QSOs");
            
            BatchUpdateLogEntries(() =>
            {
                foreach (var qso in qsos)
                    LogEntries.Add(qso);
            });
            
            if (LogEntries.Count == 0)
            {
                LoadingMessage = "No QSO records found. Add your first entry!";
                System.Diagnostics.Debug.WriteLine("No QSOs found in database");
            }
            else
            {
                LoadingMessage = $"Loaded {LogEntries.Count} QSO record(s)";
                System.Diagnostics.Debug.WriteLine($"Successfully loaded {LogEntries.Count} QSOs");
            }
            IsLoading = false;
        }
        catch (Exception ex)
        {
            LoadingMessage = $"Error loading QSOs: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error loading QSOs: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            IsLoading = false;
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Loading QSO records...";
            System.Diagnostics.Debug.WriteLine("Starting to load QSOs from database...");
            
            var qsos = await _repository.GetAllAsync();
            System.Diagnostics.Debug.WriteLine($"Database returned {qsos.Count} QSOs");
            
            BatchUpdateLogEntries(() =>
            {
                foreach (var qso in qsos)
                    LogEntries.Add(qso);
            });
            
            if (LogEntries.Count == 0)
            {
                LoadingMessage = "No QSO records found. Add your first entry!";
                System.Diagnostics.Debug.WriteLine("No QSOs found in database");
            }
            else
            {
                LoadingMessage = $"Loaded {LogEntries.Count} QSO record(s)";
                System.Diagnostics.Debug.WriteLine($"Successfully loaded {LogEntries.Count} QSOs");
            }
        }
        catch (Exception ex)
        {
            LoadingMessage = $"Error loading QSOs: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error loading QSOs: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            IsLoading = false;
        }
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
        SaveEntryAsync(newEntry);
        ClearInputs();
    }

    private async void SaveEntryAsync(Qso qso)
    {
        try
        {
            await _repository.AddAsync(qso);
            if (_repository is IUnitOfWork unitOfWork)
            {
                await unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QSO: {ex.Message}");
        }
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
        BatchUpdateLogEntries(() =>
        {
            LogEntries.Clear();
            foreach (var item in snapshot)
                LogEntries.Add(item);
        });
    }

    private void BatchUpdateLogEntries(Action action)
    {
        _isBatchUpdating = true;
        try
        {
            action();
        }
        finally
        {
            _isBatchUpdating = false;
        }

        RefreshFilter();
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
        if (_isBatchUpdating)
            return;

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
