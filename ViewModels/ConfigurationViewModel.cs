namespace HamBusLog.ViewModels;

public sealed class ConfigurationViewModel : ViewModelBase, IDisposable
{
    private readonly HamBusLog.Hardware.ISerialPortCatalogService _serialPortCatalogService;
    private AppConfiguration _appConfig = new();
    private string _selectedProfile = "default";
    private Color _backgroundColor = Color.Parse("#1F2937");
    private Color _foregroundColor = Color.Parse("#FFFFFF");
    private string _connectionString = "Data Source=hambuslog.db";
    private string _rigctldHost = "127.0.0.1";
    private int _rigctldPort = 4532;
    private string _selectedSerialPort = string.Empty;
    private string _riglistFilePath = string.Empty;
    private string _statusMessage = string.Empty;
    private string _configFilePath = string.Empty;
    private string _newProfileName = string.Empty;

    public ConfigurationViewModel()
        : this(new HamBusLog.Hardware.SerialPortCatalogService())
    {
    }

    internal ConfigurationViewModel(HamBusLog.Hardware.ISerialPortCatalogService serialPortCatalogService)
    {
        _serialPortCatalogService = serialPortCatalogService;
        AvailableProfiles = new ObservableCollection<string>();
        AvailableSerialPorts = new ObservableCollection<string>();
        Load();
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value) && value != null)
                LoadProfile(value);
        }
    }

    public ObservableCollection<string> AvailableProfiles { get; }
    public ObservableCollection<string> AvailableSerialPorts { get; }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public Color ForegroundColor
    {
        get => _foregroundColor;
        set => SetProperty(ref _foregroundColor, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public string RigctldHost
    {
        get => _rigctldHost;
        set => SetProperty(ref _rigctldHost, value);
    }

    public int RigctldPort
    {
        get => _rigctldPort;
        set => SetProperty(ref _rigctldPort, value);
    }

    public string SelectedSerialPort
    {
        get => _selectedSerialPort;
        set => SetProperty(ref _selectedSerialPort, value ?? string.Empty);
    }

    public string RiglistFilePath
    {
        get => _riglistFilePath;
        set => SetProperty(ref _riglistFilePath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ConfigFilePath
    {
        get => _configFilePath;
        private set => SetProperty(ref _configFilePath, value);
    }

    public string NewProfileName
    {
        get => _newProfileName;
        set => SetProperty(ref _newProfileName, value);
    }

    public RigCatalogViewModel RigCatalog { get; } = new();

    public void Save()
    {
        try
        {
            var profile = new ConfigProfile
            {
                Name = _selectedProfile,
                BackgroundColor = ToHexRgb(BackgroundColor),
                ForegroundColor = ToHexRgb(ForegroundColor),
                ConnectionString = string.IsNullOrWhiteSpace(ConnectionString) ? "Data Source=hambuslog.db" : ConnectionString.Trim(),
                Rigctld = new RigctldConfiguration
                {
                    Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim(),
                    Port = RigctldPort <= 0 ? 4532 : RigctldPort,
                    SerialPortName = SelectedSerialPort.Trim(),
                    RiglistFilePath = RiglistFilePath.Trim()
                }
            };

            _appConfig.Profiles[_selectedProfile] = profile;
            _appConfig.ActiveProfile = _selectedProfile;
            AppConfigurationStore.Save(_appConfig);
            StatusMessage = $"✓ Profile '{_selectedProfile}' saved at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Save failed: {ex.Message}";
        }
    }

    public void CloneProfile()
    {
        var cloneName = string.IsNullOrWhiteSpace(NewProfileName)
            ? $"{_selectedProfile}-copy"
            : NewProfileName.Trim();

        if (_appConfig.Profiles.ContainsKey(cloneName))
        {
            StatusMessage = $"✗ Profile '{cloneName}' already exists.";
            return;
        }

        var src = _appConfig.Profiles.TryGetValue(_selectedProfile, out var s)
            ? s : new ConfigProfile { Name = _selectedProfile };

        var clone = new ConfigProfile
        {
            Name = cloneName,
            BackgroundColor = ToHexRgb(BackgroundColor),
            ForegroundColor = ToHexRgb(ForegroundColor),
            ConnectionString = src.ConnectionString,
            Rigctld = new RigctldConfiguration
            {
                Host = src.Rigctld.Host,
                Port = src.Rigctld.Port,
                SerialPortName = src.Rigctld.SerialPortName,
                RiglistFilePath = src.Rigctld.RiglistFilePath
            }
        };

        _appConfig.Profiles[cloneName] = clone;
        AvailableProfiles.Add(cloneName);
        SelectedProfile = cloneName;
        NewProfileName = string.Empty;
        StatusMessage = $"✓ Profile '{cloneName}' cloned from '{src.Name}'.";
    }

    private void LoadProfile(string profileName)
    {
        if (!_appConfig.Profiles.TryGetValue(profileName, out var profile)) return;

        try { BackgroundColor = Color.Parse(profile.BackgroundColor); }
        catch { BackgroundColor = Color.Parse("#1F2937"); }

        try { ForegroundColor = Color.Parse(profile.ForegroundColor); }
        catch { ForegroundColor = Color.Parse("#FFFFFF"); }

        ConnectionString = profile.ConnectionString;
        RigctldHost = profile.Rigctld.Host;
        RigctldPort = profile.Rigctld.Port;
        SelectedSerialPort = profile.Rigctld.SerialPortName;
        RiglistFilePath = profile.Rigctld.RiglistFilePath;
        RefreshSerialPorts();
    }

    public void RefreshSerialPorts()
    {
        var discovered = _serialPortCatalogService.GetAvailablePorts();
        var currentSelection = SelectedSerialPort;

        AvailableSerialPorts.Clear();
        foreach (var port in discovered)
            AvailableSerialPorts.Add(port);

        if (!string.IsNullOrWhiteSpace(currentSelection) && !AvailableSerialPorts.Contains(currentSelection))
            AvailableSerialPorts.Insert(0, currentSelection);

        OnPropertyChanged(nameof(AvailableSerialPorts));
    }

    private void Load()
    {
        _appConfig = AppConfigurationStore.Load();
        AvailableProfiles.Clear();
        foreach (var key in _appConfig.Profiles.Keys)
            AvailableProfiles.Add(key);
        _selectedProfile = _appConfig.ActiveProfile;
        OnPropertyChanged(nameof(SelectedProfile));
        LoadProfile(_selectedProfile);
        ConfigFilePath = AppConfigurationStore.GetConfigFilePath();
    }

    private static string ToHexRgb(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public void Dispose()
    {
        RigCatalog.Dispose();
    }
}

