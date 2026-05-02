namespace HamBusLog.ViewModels;

using Avalonia.Media;
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
    private readonly IRigctldConnectionManager _rigctldConnectionManager;
    private ObservableCollection<ActiveRadioOption> _availableRadios = [];
    private ActiveRadioOption? _selectedActiveRadio;
    private ObservableCollection<RadioConnectionStatusViewModel> _radioStatuses = [];
    private RadioConnectionStatusViewModel? _selectedRadioStatus;
    private string _controlFrequencyMhz = string.Empty;
    private string _controlMode = string.Empty;
    private string _radioControlMessage = string.Empty;
    private string _radioStatusSummary = "Rig status: none";

    public MainWindowViewModel()
    {
        _rigCatalogStore = App.RigCatalogStore;
        _rigctldConnectionManager = App.RigctldConnectionManager;
        _rigCatalogStore.PropertyChanged += OnRigCatalogStorePropertyChanged;
        _rigctldConnectionManager.StatesChanged += OnRigctldStatesChanged;
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
    {
        get => _radioStatusSummary;
        private set => SetProperty(ref _radioStatusSummary, value);
    }

    public RadioConnectionStatusViewModel? SelectedRadioStatus
    {
        get => _selectedRadioStatus;
        set
        {
            var previousRadioName = _selectedRadioStatus?.RadioName;
            var previousCanControl = _selectedRadioStatus?.IsConnected == true;
            if (!SetProperty(ref _selectedRadioStatus, value))
                return;

            var selectedRadioChanged = !string.Equals(previousRadioName, value?.RadioName, StringComparison.OrdinalIgnoreCase);

            if (value is not null)
            {
                if (selectedRadioChanged)
                {
                    ControlMode = string.IsNullOrWhiteSpace(value.Mode) || value.Mode == "-"
                        ? string.Empty
                        : value.Mode;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_controlMode) && !string.IsNullOrWhiteSpace(value.Mode) && value.Mode != "-")
                        ControlMode = value.Mode;
                }
            }
            else if (selectedRadioChanged)
            {
                ControlMode = string.Empty;
            }

            var newCanControl = value?.IsConnected == true;
            if (newCanControl != previousCanControl)
                OnPropertyChanged(nameof(CanControlSelectedRadio));
        }
    }

    public bool CanControlSelectedRadio => SelectedRadioStatus?.IsConnected == true;

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

        var snapshotByName = snapshot
            .GroupBy(x => x.RadioName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        var activeRadios = rigctld.Radios.Where(r => r.IsActive).ToList();
        var activeRadioNames = new HashSet<string>(activeRadios.Select(r => r.RadioName), StringComparer.OrdinalIgnoreCase);

        var rows = activeRadios
            .Select((radio, index) =>
            {
                if (snapshotByName.TryGetValue(radio.RadioName, out var state))
                    return BuildStatusRow(index + 1, state.Label, state.RadioName, rigctld.ActiveRigNum, radio.Port, state.Mode, state.IsConnected, state.Error);

                return BuildStatusRow(index + 1, radio.RadioName, radio.RadioName, rigctld.ActiveRigNum, radio.Port, null, false, $"Not connected ({radio.Host}:{radio.Port})");
            })
            .ToList();

        foreach (var state in snapshot)
        {
            if (!activeRadioNames.Contains(state.RadioName))
                continue;

            if (rows.Any(x => string.Equals(x.RadioName, state.RadioName, StringComparison.OrdinalIgnoreCase)))
                continue;

            rows.Add(BuildStatusRow(rows.Count + 1, state.Label, state.RadioName, rigctld.ActiveRigNum, null, state.Mode, state.IsConnected, state.Error));
        }

        // Update items in-place to avoid resetting DataGrid selection and scroll position
        var prevSelectedName = SelectedRadioStatus?.RadioName;

        // Remove rows no longer present
        for (var i = _radioStatuses.Count - 1; i >= 0; i--)
        {
            if (!rows.Any(r => string.Equals(r.RadioName, _radioStatuses[i].RadioName, StringComparison.OrdinalIgnoreCase)))
                _radioStatuses.RemoveAt(i);
        }

        // Add or update rows
        for (var i = 0; i < rows.Count; i++)
        {
            var newRow = rows[i];
            var existingIndex = -1;
            for (var j = 0; j < _radioStatuses.Count; j++)
            {
                if (string.Equals(_radioStatuses[j].RadioName, newRow.RadioName, StringComparison.OrdinalIgnoreCase))
                {
                    existingIndex = j;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                // Insert at correct position
                if (i <= _radioStatuses.Count)
                    _radioStatuses.Insert(i, newRow);
                else
                    _radioStatuses.Add(newRow);
            }
            else
            {
                // Replace in-place if data changed
                _radioStatuses[existingIndex] = newRow;

                // Move to correct index if needed
                if (existingIndex != i && i < _radioStatuses.Count)
                    _radioStatuses.Move(existingIndex, i);
            }
        }

        // Restore selection, or auto-select first connected radio (or first radio if none connected)
        if (prevSelectedName is not null)
        {
            var restoredRow = _radioStatuses.FirstOrDefault(x =>
                string.Equals(x.RadioName, prevSelectedName, StringComparison.OrdinalIgnoreCase));
            if (restoredRow is not null && !ReferenceEquals(SelectedRadioStatus, restoredRow))
                SelectedRadioStatus = restoredRow;
        }
        else if (SelectedRadioStatus is null && _radioStatuses.Count > 0)
        {
            SelectedRadioStatus = _radioStatuses.FirstOrDefault(x => x.IsConnected)
                                  ?? _radioStatuses[0];
        }

        OnPropertyChanged(nameof(HasRadioStatuses));
        OnPropertyChanged(nameof(HasNoRadioStatuses));
        OnPropertyChanged(nameof(RadioStatusSummary));

        var primary = SelectedRadioStatus
                      ?? rows.FirstOrDefault(x => x.Status.StartsWith("Connected", StringComparison.OrdinalIgnoreCase))
                      ?? rows.FirstOrDefault();

        if (primary is null)
        {
            var none = "Rig status: none";
            RadioStatusSummary = none;
            return;
        }

        var mode = primary.Mode == "-" ? string.Empty : $" {primary.Mode}";
        var model = primary.RigModel == "-" ? string.Empty : $" model {primary.RigModel}";
        var endpoint = primary.ListenPort == "-" ? string.Empty : $" port {primary.ListenPort}";
        var summary = $"Rig: {primary.Label}{model}{endpoint}{mode} - {primary.Status}";
        RadioStatusSummary = summary;
    }

    public async Task ApplyFrequencyToSelectedRadioAsync()
    {
        if (SelectedRadioStatus is null)
        {
            RadioControlMessage = "Select a radio first.";
            return;
        }

        if (!SelectedRadioStatus.IsConnected)
        {
            RadioControlMessage = $"{SelectedRadioStatus.Label} is not connected.";
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
            ControlFrequencyMhz = FormatFrequencyForEditor(mhz);
            RadioControlMessage = await _rigctldConnectionManager.SetFrequencyByNameAsync(SelectedRadioStatus.RadioName, mhz, cts.Token);
        }
        catch (TimeoutException)
        {
            RadioControlMessage = "Frequency update timed out. Verify the radio is connected and try again.";
        }
        catch (Exception ex)
        {
            RadioControlMessage = "Frequency update failed: " + ex.Message;
        }
    }

    public async Task ApplyModeToSelectedRadioAsync()
    {
        await ApplyModeToSelectedRadioAsync(ControlMode);
    }

    private async Task ApplyModeToSelectedRadioAsync(string modeToApply)
    {
        if (SelectedRadioStatus is null)
        {
            RadioControlMessage = "Select a radio first.";
            return;
        }

        if (!SelectedRadioStatus.IsConnected)
        {
            RadioControlMessage = $"{SelectedRadioStatus.Label} is not connected.";
            return;
        }

        if (string.IsNullOrWhiteSpace(modeToApply))
        {
            RadioControlMessage = "Enter a mode (USB, LSB, CW, FM, AM...).";
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            RadioControlMessage = await _rigctldConnectionManager.SetModeByNameAsync(SelectedRadioStatus.RadioName, modeToApply, cts.Token);
        }
        catch (TimeoutException)
        {
            RadioControlMessage = "Mode update timed out. Verify the radio is connected and try again.";
        }
        catch (Exception ex)
        {
            RadioControlMessage = "Mode update failed: " + ex.Message;
        }
    }

    public async Task ApplyPresetModeToSelectedRadioAsync(string mode)
    {
        ControlMode = mode;
        await ApplyModeToSelectedRadioAsync(mode);
    }

    private static string FormatFrequencyForEditor(decimal? mhz)
        => mhz is decimal value && value > 0
            ? value.ToString("0.000", CultureInfo.InvariantCulture)
            : string.Empty;

     private static RadioConnectionStatusViewModel BuildStatusRow(
         int rowNumber,
         string label,
          string radioName,
          int? rigModelNumber,
          int? listenPort,
         string? mode,
         bool isConnected,
         string? error)
     {
         return new RadioConnectionStatusViewModel(
             rowNumber,
             label,
             radioName,
             rigModelNumber,
             listenPort,
             NormalizeModeForDisplay(mode),
             isConnected,
             isConnected
                 ? "Connected"
                 : string.IsNullOrWhiteSpace(error)
                     ? "Not connected"
                     : "Not connected: " + error);
     }

    private static string NormalizeModeForDisplay(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return "-";

        return mode.Trim().ToUpperInvariant() switch
        {
            "PKTUSB" => "DIGU",
            "PKTLSB" => "DIGL",
            var x => x
        };
    }

    public void Dispose()
    {
        _rigCatalogStore.PropertyChanged -= OnRigCatalogStorePropertyChanged;
        _rigctldConnectionManager.StatesChanged -= OnRigctldStatesChanged;
    }
}

public sealed class RadioConnectionStatusViewModel
{
    public RadioConnectionStatusViewModel(int rowNumber, string label, string radioName, int? rigModelNumber, int? listenPort, string mode, bool isConnected, string status)
    {
        RowNumber = rowNumber;
        Label = label;
        RadioName = radioName;
        RigModel = rigModelNumber?.ToString(CultureInfo.InvariantCulture) ?? "-";
        ListenPort = listenPort?.ToString(CultureInfo.InvariantCulture) ?? "-";
        Mode = mode;
        IsConnected = isConnected;
        Status = status;
        RowBackground = isConnected
            ? new SolidColorBrush(Color.Parse("#1E3A2F"))   // dark green tint
            : new SolidColorBrush(Color.Parse("#3A1E1E"));  // dark red tint
        RowForeground = new SolidColorBrush(Colors.White);
    }

    public int RowNumber { get; }
    public string Label { get; }
    public string RadioName { get; }
    public string RigModel { get; }
    public string ListenPort { get; }
    public string Mode { get; }
    public bool IsConnected { get; }
    public string Status { get; }
    public SolidColorBrush RowBackground { get; }
    public SolidColorBrush RowForeground { get; }
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
