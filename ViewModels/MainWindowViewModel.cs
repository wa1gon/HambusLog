namespace HamBusLog.ViewModels;

using Avalonia.Threading;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    public MenuNode[] MenuItems { get; } =
    [
        new MenuNode("Grid"),
        new MenuNode("Add New Contact"),
        new MenuNode("File", true,
            new MenuNode("Open/Reopen Grid"),
            new MenuNode("Import ADIF"),
            new MenuNode("Export ADIF"),
            new MenuNode("Remove Dups"),
            new MenuNode("Watch List")),
        new MenuNode("Edit"),
        new MenuNode("Configuration"),
        new MenuNode("Callbook"),
        new MenuNode("List"),
        new MenuNode("Search"),
        new MenuNode("Awards"),
        new MenuNode("eLogs"),
        new MenuNode("RecCall"),
        new MenuNode("Net View"),
        new MenuNode("Help")
    ];

    private MenuNode? _selectedMenuItem;
    private readonly RigCatalogStore _rigCatalogStore;
    private readonly HamBusLog.Hardware.RigctldConnectionManager _rigctldConnectionManager;
    private readonly DispatcherTimer _radioStatusRefreshTimer;
    private ObservableCollection<ActiveRadioOption> _availableRadios = [];
    private ActiveRadioOption? _selectedActiveRadio;
    private ObservableCollection<RadioConnectionStatusViewModel> _radioStatuses = [];
    private RadioConnectionStatusViewModel? _selectedRadioStatus;
    private string _controlFrequencyMhz = string.Empty;
    private string _controlMode = string.Empty;
    private string _radioControlMessage = string.Empty;

    public MainWindowViewModel()
    {
        _rigCatalogStore = App.RigCatalogStore;
        _rigctldConnectionManager = App.RigctldConnectionManager;
        _rigCatalogStore.PropertyChanged += OnRigCatalogStorePropertyChanged;
        _rigctldConnectionManager.StatesChanged += OnRigctldStatesChanged;
        _radioStatusRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _radioStatusRefreshTimer.Tick += OnRadioStatusRefreshTimerTick;
        _radioStatusRefreshTimer.Start();
        RefreshActiveRadioOptions();
        RefreshRadioStatuses();
        _ = _rigctldConnectionManager.RefreshActiveConnectionsAsync();
        SelectedMenuItem = MenuItems[0];
    }

    public MenuNode? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (SetProperty(ref _selectedMenuItem, value))
            {
                OnPropertyChanged(nameof(SelectedMenuTitle));
            }
        }
    }

    public string SelectedMenuTitle => SelectedMenuItem?.Title ?? "None";

    public ObservableCollection<ActiveRadioOption> AvailableRadios
    {
        get => _availableRadios;
        private set => SetProperty(ref _availableRadios, value);
    }

    public ActiveRadioOption? SelectedActiveRadio
    {
        get => _selectedActiveRadio;
        set
        {
            if (!SetProperty(ref _selectedActiveRadio, value))
                return;

            _rigCatalogStore.SetActiveRig(value?.RigNum);
            OnPropertyChanged(nameof(ActiveRadioSummary));
        }
    }

    public string ActiveRadioSummary
        => SelectedActiveRadio is null
            ? "Active Radio: none"
            : $"Active Radio: {SelectedActiveRadio.Display}";

    public ObservableCollection<RadioConnectionStatusViewModel> RadioStatuses
    {
        get => _radioStatuses;
        private set => SetProperty(ref _radioStatuses, value);
    }

    public bool HasRadioStatuses => RadioStatuses.Count > 0;
    public bool HasNoRadioStatuses => !HasRadioStatuses;

    public string RadioStatusSummary
        => HasRadioStatuses ? string.Empty : "No radios configured.";

    public RadioConnectionStatusViewModel? SelectedRadioStatus
    {
        get => _selectedRadioStatus;
        set
        {
            if (!SetProperty(ref _selectedRadioStatus, value))
                return;

            if (value is not null)
            {
                if (string.IsNullOrWhiteSpace(_controlFrequencyMhz) && value.FrequencyMhz is decimal mhz)
                    ControlFrequencyMhz = mhz.ToString("0.######", CultureInfo.InvariantCulture);

                if (string.IsNullOrWhiteSpace(_controlMode) && !string.IsNullOrWhiteSpace(value.Mode) && value.Mode != "-")
                    ControlMode = value.Mode;
            }

            OnPropertyChanged(nameof(CanControlSelectedRadio));
        }
    }

    public bool CanControlSelectedRadio => SelectedRadioStatus is not null;

    public string ControlFrequencyMhz
    {
        get => _controlFrequencyMhz;
        set => SetProperty(ref _controlFrequencyMhz, value ?? string.Empty);
    }

    public string ControlMode
    {
        get => _controlMode;
        set => SetProperty(ref _controlMode, value ?? string.Empty);
    }

    public string RadioControlMessage
    {
        get => _radioControlMessage;
        private set => SetProperty(ref _radioControlMessage, value);
    }

    private void OnRigCatalogStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RigCatalogStore.Entries) or nameof(RigCatalogStore.ActiveRigNum))
            RefreshActiveRadioOptions();
    }

    private void RefreshActiveRadioOptions()
    {
        var options = _rigCatalogStore.Entries
            .Select(entry => new ActiveRadioOption(entry.RigNum, $"{entry.RigNum} - {entry.Mfg} {entry.Model}"))
            .ToList();

        AvailableRadios = new ObservableCollection<ActiveRadioOption>(options);

        if (_rigCatalogStore.ActiveRigNum is int activeRigNum)
            SelectedActiveRadio = AvailableRadios.FirstOrDefault(x => x.RigNum == activeRigNum);
        else
            SelectedActiveRadio = AvailableRadios.FirstOrDefault();

        OnPropertyChanged(nameof(ActiveRadioSummary));
    }

    private void OnRigctldStatesChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            RefreshRadioStatuses();
            return;
        }

        Dispatcher.UIThread.Post(RefreshRadioStatuses);
    }

    private void RefreshRadioStatuses()
    {
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);
        var snapshot = _rigctldConnectionManager.GetSnapshot();

        var snapshotByTag = snapshot
            .GroupBy(x => x.TagName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        var rows = rigctld.Radios
            .Select(radio =>
            {
                if (snapshotByTag.TryGetValue(radio.TagName, out var state))
                    return BuildStatusRow(state.Label, state.TagName, state.FrequencyMhz, state.Mode, state.IsConnected, state.Error);

                var label = string.IsNullOrWhiteSpace(radio.DisplayName) ? radio.TagName : radio.DisplayName;
                return BuildStatusRow(label, radio.TagName, null, null, false, $"Not connected ({radio.Host}:{radio.Port})");
            })
            .ToList();

        foreach (var state in snapshot)
        {
            if (rows.Any(x => string.Equals(x.TagName, state.TagName, StringComparison.OrdinalIgnoreCase)))
                continue;

            rows.Add(BuildStatusRow(state.Label, state.TagName, state.FrequencyMhz, state.Mode, state.IsConnected, state.Error));
        }

        RadioStatuses = new ObservableCollection<RadioConnectionStatusViewModel>(rows);

        if (SelectedRadioStatus is not null)
        {
            SelectedRadioStatus = RadioStatuses.FirstOrDefault(x =>
                string.Equals(x.TagName, SelectedRadioStatus.TagName, StringComparison.OrdinalIgnoreCase));
        }

        OnPropertyChanged(nameof(HasRadioStatuses));
        OnPropertyChanged(nameof(HasNoRadioStatuses));
        OnPropertyChanged(nameof(RadioStatusSummary));
    }

    public async Task ApplyFrequencyToSelectedRadioAsync()
    {
        if (SelectedRadioStatus is null)
        {
            RadioControlMessage = "Select a radio first.";
            return;
        }

        if (!decimal.TryParse(ControlFrequencyMhz, NumberStyles.Number, CultureInfo.InvariantCulture, out var mhz) || mhz <= 0)
        {
            RadioControlMessage = "Enter a valid frequency in MHz.";
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            RadioControlMessage = await _rigctldConnectionManager.SetFrequencyByTagAsync(SelectedRadioStatus.TagName, mhz, cts.Token);
        }
        catch (Exception ex)
        {
            RadioControlMessage = "Frequency update failed: " + ex.Message;
        }
    }

    public async Task ApplyModeToSelectedRadioAsync()
    {
        if (SelectedRadioStatus is null)
        {
            RadioControlMessage = "Select a radio first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ControlMode))
        {
            RadioControlMessage = "Enter a mode (USB, LSB, CW, FM, AM...).";
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            RadioControlMessage = await _rigctldConnectionManager.SetModeByTagAsync(SelectedRadioStatus.TagName, ControlMode, cts.Token);
        }
        catch (Exception ex)
        {
            RadioControlMessage = "Mode update failed: " + ex.Message;
        }
    }

    public async Task ApplyPresetModeToSelectedRadioAsync(string mode)
    {
        ControlMode = mode;
        await ApplyModeToSelectedRadioAsync();
    }

    private static RadioConnectionStatusViewModel BuildStatusRow(
        string label,
        string tagName,
        decimal? frequencyMhz,
        string? mode,
        bool isConnected,
        string? error)
    {
        return new RadioConnectionStatusViewModel(
            label,
            tagName,
            frequencyMhz,
            frequencyMhz is decimal mhz
                ? mhz.ToString("0.######", CultureInfo.InvariantCulture) + " MHz"
                : "-",
            string.IsNullOrWhiteSpace(mode) ? "-" : mode,
            isConnected
                ? "Connected"
                : string.IsNullOrWhiteSpace(error)
                    ? "Not connected"
                    : "Not connected: " + error);
    }

    private void OnRadioStatusRefreshTimerTick(object? sender, EventArgs e)
    {
        RefreshRadioStatuses();
    }

    public void Dispose()
    {
        _rigCatalogStore.PropertyChanged -= OnRigCatalogStorePropertyChanged;
        _rigctldConnectionManager.StatesChanged -= OnRigctldStatesChanged;
        _radioStatusRefreshTimer.Stop();
        _radioStatusRefreshTimer.Tick -= OnRadioStatusRefreshTimerTick;
    }
}

public sealed class RadioConnectionStatusViewModel
{
    public RadioConnectionStatusViewModel(string label, string tagName, decimal? frequencyMhz, string frequency, string mode, string status)
    {
        Label = label;
        TagName = tagName;
        FrequencyMhz = frequencyMhz;
        Frequency = frequency;
        Mode = mode;
        Status = status;
    }

    public string Label { get; }
    public string TagName { get; }
    public decimal? FrequencyMhz { get; }
    public string Frequency { get; }
    public string Mode { get; }
    public string Status { get; }
}

public sealed class ActiveRadioOption
{
    public ActiveRadioOption(int rigNum, string display)
    {
        RigNum = rigNum;
        Display = display;
    }

    public int RigNum { get; }
    public string Display { get; }

    public override string ToString() => Display;
}

public sealed class MenuNode
{
    public MenuNode(string title, params MenuNode[] children)
        : this(title, false, children)
    {
    }

    public MenuNode(string title, bool isExpanded, params MenuNode[] children)
    {
        Title = title;
        IsExpanded = isExpanded;
        Children = children;
    }

    public string Title { get; }
    public bool IsExpanded { get; set; }
    public MenuNode[] Children { get; }
    public bool HasChildren => Children.Length > 0;
}
