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
        if (_store.Entries.Count > 0)
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
                RefreshFilteredEntries();
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
                UpdateCommandLine();
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
            RefreshAvailableModels();
            RefreshFilteredEntries();
        }

        if (e.PropertyName is nameof(RigCatalogStore.FilePath))
            OnPropertyChanged(nameof(FilePath));

        if (e.PropertyName is nameof(RigCatalogStore.StatusMessage))
            OnPropertyChanged(nameof(StatusMessage));
    }

    private void RefreshFilteredEntries()
    {
        var searchModel = SelectedSearchModel == AllModelsOption ? null : SelectedSearchModel;
        var filtered = RigctldRadioCatalogService.FilterByModel(_store.Entries, searchModel);
        FilteredEntries = new ObservableCollection<RigCatalogEntry>(filtered);

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
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        RigctldCommandLine = RigctldRadioCatalogService.CreateRigctldCommandLine(
            SelectedEntry,
            profile.Rigctld.Host,
            profile.Rigctld.Port,
            SelectedSerialPort);
        _statusMessageOverride = null;
        OnPropertyChanged(nameof(StatusMessage));
    }

    public void Dispose()
    {
        _store.PropertyChanged -= OnStorePropertyChanged;
    }
}







