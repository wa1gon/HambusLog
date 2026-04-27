namespace HamBusLog.ViewModels;

public sealed class RigCatalogViewModel : ViewModelBase, IDisposable
{
    private readonly RigCatalogStore _store;
    private readonly HamBusLog.Hardware.ISerialPortCatalogService _serialPortCatalogService;
    private ObservableCollection<RigCatalogEntry> _filteredEntries = [];
    private ObservableCollection<string> _availableSerialPorts = [];
    private string _searchModelText = string.Empty;
    private string _selectedSerialPort = string.Empty;
    private string _rigctldExecutable = "rigctld";
    private string _rigctldHost = "127.0.0.1";
    private int _rigctldPort = 4532;
    private string _rigctldArgumentsTemplate = "-m {rigNum} -T {host} -t {port}{serialArg}";
    private RigCatalogEntry? _selectedEntry;
    private string _rigctldCommandLine = string.Empty;
    private string? _statusMessageOverride;

    public RigCatalogViewModel()
        : this(App.RigCatalogStore, new HamBusLog.Hardware.SerialPortCatalogService())
    {
    }

    internal RigCatalogViewModel(RigCatalogStore store, HamBusLog.Hardware.ISerialPortCatalogService serialPortCatalogService)
    {
        _store = store;
        _serialPortCatalogService = serialPortCatalogService;
        _store.PropertyChanged += OnStorePropertyChanged;
        ReloadFromConfiguration();

        RefreshSerialPorts();
        RefreshFilteredEntries();
        if (SelectedEntry is null && _store.Entries.Count > 0)
            SelectedEntry = _store.Entries[0];
    }

    public void ReloadFromConfiguration()
    {
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);
        var radio = AppConfigurationStore.GetRigctldRadio(rigctld, rigctld.ActiveRadioTag);
        var configuredPath = radio.RiglistFilePath;
        _rigctldExecutable = string.IsNullOrWhiteSpace(radio.Executable) ? "rigctld" : radio.Executable;
        _rigctldHost = string.IsNullOrWhiteSpace(radio.Host) ? "127.0.0.1" : radio.Host;
        _rigctldPort = radio.Port <= 0 ? 4532 : radio.Port;
        _rigctldArgumentsTemplate = string.IsNullOrWhiteSpace(radio.ArgumentsTemplate)
            ? "-m {rigNum} -T {host} -t {port}{serialArg}"
            : radio.ArgumentsTemplate;
        _selectedSerialPort = radio.SerialPortName;
        OnPropertyChanged(nameof(RigctldExecutable));
        OnPropertyChanged(nameof(RigctldHost));
        OnPropertyChanged(nameof(RigctldPort));
        OnPropertyChanged(nameof(RigctldArgumentsTemplate));
        OnPropertyChanged(nameof(SelectedSerialPort));
        if (!string.IsNullOrWhiteSpace(configuredPath) && !string.Equals(configuredPath, _store.FilePath, StringComparison.Ordinal))
            _store.LoadFromFile(configuredPath);
    }

    public ObservableCollection<RigCatalogEntry> FilteredEntries
    {
        get => _filteredEntries;
        private set => SetProperty(ref _filteredEntries, value);
    }

    public ObservableCollection<string> AvailableSerialPorts
    {
        get => _availableSerialPorts;
        private set => SetProperty(ref _availableSerialPorts, value);
    }

    public string SearchModelText
    {
        get => _searchModelText;
        set
        {
            if (!SetProperty(ref _searchModelText, value ?? string.Empty))
                return;

            RefreshFilteredEntries();
        }
    }

    public string SelectedSerialPort
    {
        get => _selectedSerialPort;
        set
        {
            if (SetProperty(ref _selectedSerialPort, value ?? string.Empty))
            {
                SaveRigctldSettings();
                UpdateCommandLine();
            }
        }
    }

    public string RigctldExecutable
    {
        get => _rigctldExecutable;
        set
        {
            if (SetProperty(ref _rigctldExecutable, value ?? string.Empty))
            {
                SaveRigctldSettings();
                UpdateCommandLine();
            }
        }
    }

    public string RigctldHost
    {
        get => _rigctldHost;
        set
        {
            if (SetProperty(ref _rigctldHost, value ?? string.Empty))
            {
                SaveRigctldSettings();
                UpdateCommandLine();
            }
        }
    }

    public int RigctldPort
    {
        get => _rigctldPort;
        set
        {
            if (SetProperty(ref _rigctldPort, value))
            {
                SaveRigctldSettings();
                UpdateCommandLine();
            }
        }
    }

    public string RigctldArgumentsTemplate
    {
        get => _rigctldArgumentsTemplate;
        set
        {
            if (SetProperty(ref _rigctldArgumentsTemplate, value ?? string.Empty))
            {
                SaveRigctldSettings();
                UpdateCommandLine();
            }
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
        var typedSearch = SearchModelText?.Trim();
        var searchModel = string.IsNullOrWhiteSpace(typedSearch) ? null : typedSearch;
        var filtered = RigctldRadioCatalogService.FilterByModel(_store.Entries, searchModel);
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

    private void UpdateCommandLine()
    {
        var selectedRigNums = _store.ActiveRigNums.ToList();
        if (selectedRigNums.Count > 1)
        {
            var lines = selectedRigNums
                .Select(rigNum => _store.Entries.FirstOrDefault(x => x.RigNum == rigNum))
                .Where(entry => entry is not null)
                .Select(entry => RigctldRadioCatalogService.CreateRigctldCommandLine(
                    entry,
                    RigctldHost,
                    RigctldPort,
                    SelectedSerialPort,
                    RigctldExecutable,
                    RigctldArgumentsTemplate))
                .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
                .ToList();
            RigctldCommandLine = string.Join(Environment.NewLine, lines);
        }
        else
        {
            RigctldCommandLine = RigctldRadioCatalogService.CreateRigctldCommandLine(
                SelectedEntry,
                RigctldHost,
                RigctldPort,
                SelectedSerialPort,
                RigctldExecutable,
                RigctldArgumentsTemplate);
        }
        _statusMessageOverride = null;
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void SaveRigctldSettings()
    {
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);
        var radio = AppConfigurationStore.GetRigctldRadio(rigctld, rigctld.ActiveRadioTag);
        radio.Executable = string.IsNullOrWhiteSpace(RigctldExecutable) ? "rigctld" : RigctldExecutable.Trim();
        radio.ArgumentsTemplate = string.IsNullOrWhiteSpace(RigctldArgumentsTemplate)
            ? "-m {rigNum} -T {host} -t {port}{serialArg}"
            : RigctldArgumentsTemplate;
        radio.Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim();
        radio.Port = RigctldPort <= 0 ? 4532 : RigctldPort;
        radio.SerialPortName = SelectedSerialPort.Trim();
        AppConfigurationStore.Save(config);
    }

    public void Dispose()
    {
        _store.PropertyChanged -= OnStorePropertyChanged;
    }

    public void SetSelectedEntries(IEnumerable<RigCatalogEntry>? selectedEntries)
    {
        var selected = (selectedEntries ?? [])
            .Select(x => x.RigNum)
            .Distinct()
            .ToList();

        _store.SetActiveRigs(selected);
        if (selected.Count > 0)
        {
            var first = FilteredEntries.FirstOrDefault(x => x.RigNum == selected[0])
                        ?? _store.Entries.FirstOrDefault(x => x.RigNum == selected[0]);
            if (first is not null && !ReferenceEquals(SelectedEntry, first))
                SelectedEntry = first;
            else
                UpdateCommandLine();
        }
        else
        {
            UpdateCommandLine();
        }
    }
}
