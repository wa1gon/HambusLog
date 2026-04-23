namespace HamBusLog.ViewModels;

public sealed class RigCatalogViewModel : ViewModelBase, IDisposable
{
    private readonly RigCatalogStore _store;
    private ObservableCollection<RigCatalogEntry> _filteredEntries = [];
    private string _searchText = string.Empty;
    private RigCatalogEntry? _selectedEntry;
    private string _rigctldCommandLine = string.Empty;
    private string? _statusMessageOverride;

    public RigCatalogViewModel()
        : this(App.RigCatalogStore)
    {
    }

    internal RigCatalogViewModel(RigCatalogStore store)
    {
        _store = store;
        _store.PropertyChanged += OnStorePropertyChanged;

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var configuredPath = profile.Rigctld.RiglistFilePath;
        if (!string.IsNullOrWhiteSpace(configuredPath) && !string.Equals(configuredPath, _store.FilePath, StringComparison.Ordinal))
        {
            _store.LoadFromFile(configuredPath);
        }

        RefreshFilteredEntries();
        if (_store.Entries.Count > 0)
            SelectedEntry = _store.Entries[0];
    }

    public ObservableCollection<RigCatalogEntry> FilteredEntries
    {
        get => _filteredEntries;
        private set => SetProperty(ref _filteredEntries, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RefreshFilteredEntries();
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

    private void OnStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RigCatalogStore.Entries))
        {
            RefreshFilteredEntries();
        }

        if (e.PropertyName is nameof(RigCatalogStore.FilePath))
            OnPropertyChanged(nameof(FilePath));

        if (e.PropertyName is nameof(RigCatalogStore.StatusMessage))
            OnPropertyChanged(nameof(StatusMessage));
    }

    private void RefreshFilteredEntries()
    {
        var filtered = RigctldRadioCatalogService.FilterByModel(_store.Entries, SearchText);
        FilteredEntries = new ObservableCollection<RigCatalogEntry>(filtered);

        if (SelectedEntry is not null && FilteredEntries.Any(x => x.RigNum == SelectedEntry.RigNum))
        {
            UpdateCommandLine();
            return;
        }

        SelectedEntry = FilteredEntries.FirstOrDefault();
    }

    private void UpdateCommandLine()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        RigctldCommandLine = RigctldRadioCatalogService.CreateRigctldCommandLine(
            SelectedEntry,
            profile.Rigctld.Host,
            profile.Rigctld.Port);
        _statusMessageOverride = null;
        OnPropertyChanged(nameof(StatusMessage));
    }

    public void Dispose()
    {
        _store.PropertyChanged -= OnStorePropertyChanged;
    }
}




