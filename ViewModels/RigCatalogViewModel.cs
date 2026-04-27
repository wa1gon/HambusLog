namespace HamBusLog.ViewModels;

public sealed class RigCatalogViewModel : ViewModelBase, IDisposable
{
    private const string AllModelsOption = "All Models";

    private readonly RigCatalogStore _store;
    private readonly HamBusLog.Hardware.ISerialPortCatalogService _serialPortCatalogService;
    private ObservableCollection<RigCatalogEntry> _filteredEntries = [];
    private ObservableCollection<string> _availableModels = [];
    private ObservableCollection<string> _availableSerialPorts = [];
    private string _selectedSearchModel = AllModelsOption;
    private string _selectedSerialPort = string.Empty;
    private RigCatalogEntry? _selectedEntry;
    private string _rigctldCommandLine = string.Empty;
    private string? _statusMessageOverride;

    private string _searchModelText = string.Empty;
    private string _rigctldExecutable = "rigctld";
    private string _rigctldArgumentsTemplate = "-m {rigNum} -T {host} -t {port}{serialArg}";
    private string _rigctldHost = "127.0.0.1";
    private int _rigctldPort = 4532;

    public RigCatalogViewModel()
        : this(App.RigCatalogStore, new HamBusLog.Hardware.SerialPortCatalogService())
    {
    }

    internal RigCatalogViewModel(RigCatalogStore store, HamBusLog.Hardware.ISerialPortCatalogService serialPortCatalogService)
    {
        _store = store;
        _serialPortCatalogService = serialPortCatalogService;
        _store.PropertyChanged += OnStorePropertyChanged;

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var configuredPath = profile.Rigctld.RiglistFilePath;
        _selectedSerialPort = profile.Rigctld.SerialPortName;
        if (!string.IsNullOrWhiteSpace(configuredPath) && !string.Equals(configuredPath, _store.FilePath, StringComparison.Ordinal))
        {
            _store.LoadFromFile(configuredPath);
        }

        RefreshSerialPorts();
        RefreshAvailableModels();
        RefreshFilteredEntries();
        if (SelectedEntry is null && _store.Entries.Count > 0)
            SelectedEntry = _store.Entries[0];
    }

    public ObservableCollection<RigCatalogEntry> FilteredEntries
    {
        get => _filteredEntries;
        private set => SetProperty(ref _filteredEntries, value);
    }

    public ObservableCollection<string> AvailableModels
    {
        get => _availableModels;
        private set => SetProperty(ref _availableModels, value);
    }

    public ObservableCollection<string> AvailableSerialPorts
    {
        get => _availableSerialPorts;
        private set => SetProperty(ref _availableSerialPorts, value);
    }

    public string SelectedSearchModel
    {
        get => _selectedSearchModel;
        set
        {
            if (SetProperty(ref _selectedSearchModel, value))
            {
                _searchModelText = string.Equals(value, AllModelsOption, StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : value;
                OnPropertyChanged(nameof(SearchModelText));
                RefreshFilteredEntries();
            }
        }
    }

    public string SearchModelText
    {
        get => _searchModelText;
        set
        {
            if (SetProperty(ref _searchModelText, value ?? string.Empty))
            {
                var normalized = string.IsNullOrWhiteSpace(_searchModelText) ? AllModelsOption : _searchModelText.Trim();
                if (!string.Equals(_selectedSearchModel, normalized, StringComparison.Ordinal))
                {
                    _selectedSearchModel = normalized;
                    OnPropertyChanged(nameof(SelectedSearchModel));
                }
                RefreshFilteredEntries();
            }
        }
    }

    public string SelectedSerialPort
    {
        get => _selectedSerialPort;
        set
        {
            if (SetProperty(ref _selectedSerialPort, value ?? string.Empty))
                UpdateCommandLine();
        }
    }

    public RigCatalogEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                _store.SetActiveRig(value?.RigNum);
                UpdateCommandLine();
            }
        }
    }

    public string RigctldCommandLine
    {
        get => _rigctldCommandLine;
        private set => SetProperty(ref _rigctldCommandLine, value);
    }

    public string FilePath => _store.FilePath;
    public string StatusMessage => _statusMessageOverride ?? _store.StatusMessage;

    public string RigctldExecutable
    {
        get => _rigctldExecutable;
        set
        {
            if (SetProperty(ref _rigctldExecutable, value ?? string.Empty))
                UpdateCommandLine();
        }
    }

    public string RigctldArgumentsTemplate
    {
        get => _rigctldArgumentsTemplate;
        set
        {
            if (SetProperty(ref _rigctldArgumentsTemplate, value ?? string.Empty))
                UpdateCommandLine();
        }
    }

    public string RigctldHost
    {
        get => _rigctldHost;
        set
        {
            if (SetProperty(ref _rigctldHost, value ?? string.Empty))
                UpdateCommandLine();
        }
    }

    public int RigctldPort
    {
        get => _rigctldPort;
        set
        {
            if (SetProperty(ref _rigctldPort, value <= 0 ? 4532 : value))
                UpdateCommandLine();
        }
    }

    public void SetStatusMessage(string message)
    {
        _statusMessageOverride = message;
        OnPropertyChanged(nameof(StatusMessage));
    }

    public void LoadFromFile(string path)
    {
        _statusMessageOverride = null;
        OnPropertyChanged(nameof(StatusMessage));
        _store.LoadFromFile(path);
    }

    public void Reload()
    {
        if (!string.IsNullOrWhiteSpace(_store.FilePath))
        {
            _statusMessageOverride = null;
            OnPropertyChanged(nameof(StatusMessage));
            _store.LoadFromFile(_store.FilePath);
        }
    }

    public void RefreshSerialPorts()
    {
        var currentSelection = SelectedSerialPort;
        var discovered = _serialPortCatalogService.GetAvailablePorts();
        AvailableSerialPorts = new ObservableCollection<string>(discovered);

        if (!string.IsNullOrWhiteSpace(currentSelection) && !AvailableSerialPorts.Contains(currentSelection))
            AvailableSerialPorts.Insert(0, currentSelection);

        OnPropertyChanged(nameof(AvailableSerialPorts));
    }

    private void OnStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RigCatalogStore.Entries))
        {
            RefreshAvailableModels();
            RefreshFilteredEntries();
        }

        if (e.PropertyName is nameof(RigCatalogStore.ActiveRigNum))
        {
            var active = _store.ActiveRigNum;
            if (active is int rigNum)
            {
                var match = FilteredEntries.FirstOrDefault(x => x.RigNum == rigNum)
                            ?? _store.Entries.FirstOrDefault(x => x.RigNum == rigNum);
                if (match is not null && !ReferenceEquals(SelectedEntry, match))
                    SelectedEntry = match;
            }
        }

        if (e.PropertyName is nameof(RigCatalogStore.FilePath))
            OnPropertyChanged(nameof(FilePath));

        if (e.PropertyName is nameof(RigCatalogStore.StatusMessage))
            OnPropertyChanged(nameof(StatusMessage));
    }

    private void RefreshFilteredEntries()
    {
        var term = SearchModelText;
        var filtered = RigctldRadioCatalogService.FilterByModel(_store.Entries, term);
        FilteredEntries = new ObservableCollection<RigCatalogEntry>(filtered);

        if (_store.ActiveRigNum is int activeRigNum)
        {
            var active = FilteredEntries.FirstOrDefault(x => x.RigNum == activeRigNum);
            if (active is not null)
            {
                SelectedEntry = active;
                return;
            }
        }

        if (SelectedEntry is not null && FilteredEntries.Any(x => x.RigNum == SelectedEntry.RigNum))
        {
            UpdateCommandLine();
            return;
        }

        SelectedEntry = FilteredEntries.FirstOrDefault();
    }

    private void RefreshAvailableModels()
    {
        var models = _store.Entries
            .Select(x => x.Model)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var available = new ObservableCollection<string> { AllModelsOption };
        foreach (var model in models)
            available.Add(model);

        AvailableModels = available;

        if (!AvailableModels.Contains(SelectedSearchModel))
            SelectedSearchModel = AllModelsOption;
    }

    private void UpdateCommandLine()
    {
        RigctldCommandLine = RigctldRadioCatalogService.CreateRigctldCommandLine(
            SelectedEntry,
            RigctldHost,
            RigctldPort,
            SelectedSerialPort,
            RigctldExecutable,
            RigctldArgumentsTemplate);
        _statusMessageOverride = null;
        OnPropertyChanged(nameof(StatusMessage));
    }

    public void SetSelectedEntries(IEnumerable<RigCatalogEntry> entries)
    {
        var first = entries?.FirstOrDefault();
        SelectedEntry = first;
        _store.SetActiveRig(first?.RigNum);
    }

    public void ReloadFromConfiguration()
    {
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);
        var radio = AppConfigurationStore.GetRigctldRadio(rigctld, rigctld.ActiveRadioTag);

        RigctldExecutable = string.IsNullOrWhiteSpace(radio.Executable) ? "rigctld" : radio.Executable;
        RigctldArgumentsTemplate = string.IsNullOrWhiteSpace(radio.ArgumentsTemplate)
            ? "-m {rigNum} -T {host} -t {port}{serialArg}"
            : radio.ArgumentsTemplate;
        RigctldHost = string.IsNullOrWhiteSpace(radio.Host) ? rigctld.Host : radio.Host;
        RigctldPort = radio.Port <= 0 ? rigctld.Port : radio.Port;
        SelectedSerialPort = string.IsNullOrWhiteSpace(radio.SerialPortName) ? rigctld.SerialPortName : radio.SerialPortName;

        var configuredPath = string.IsNullOrWhiteSpace(radio.RiglistFilePath) ? rigctld.RiglistFilePath : radio.RiglistFilePath;
        if (!string.IsNullOrWhiteSpace(configuredPath) && !string.Equals(configuredPath, _store.FilePath, StringComparison.Ordinal))
            _store.LoadFromFile(configuredPath);

        UpdateCommandLine();
    }

    public void Dispose()
    {
        _store.PropertyChanged -= OnStorePropertyChanged;
    }
}









