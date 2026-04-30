namespace HamBusLog.ViewModels;

public sealed class ConfigurationViewModel : ViewModelBase, IDisposable
{
    private readonly HamBusLog.Hardware.ISerialPortCatalogService _serialPortCatalogService;
    private AppConfiguration _appConfig = new();
    private string _selectedProfile = "default";
    private Color _backgroundColor = Color.Parse("#1F2937");
    private Color _foregroundColor = Color.Parse("#FFFFFF");
    private Color _menuBackgroundColor = Color.Parse("#111827");
    private Color _menuForegroundColor = Color.Parse("#FFFFFF");
    private Color _buttonNormalColor = Color.Parse("#2563EB");
    private Color _buttonNormalForegroundColor = Color.Parse("#FFFFFF");
    private Color _buttonCautionColor = Color.Parse("#D97706");
    private Color _buttonCautionForegroundColor = Color.Parse("#FFFFFF");
    private Color _buttonDangerColor = Color.Parse("#DC2626");
    private Color _buttonDangerForegroundColor = Color.Parse("#FFFFFF");
    private Color _buttonForegroundColor = Color.Parse("#FFFFFF");
    private Color _inputBackgroundColor = Color.Parse("#2C3E50");
    private Color _inputForegroundColor = Color.Parse("#FFFFFF");
    private Color _inputBorderColor = Color.Parse("#34495E");
    private Color _inputSelectionBackgroundColor = Color.Parse("#2C3E50");
    private Color _inputSelectionForegroundColor = Color.Parse("#FFFFFF");
    private string _adifDirectory = string.Empty;
    private string _databaseFolderPath = string.Empty;
    private string _databaseFileName = "hambuslog.db";
    private string _connectionString = "Data Source=hambuslog.db";
    private string _selectedRigRadioTag = "radio-1";
    private string _rigctldRadioName = "radio-1";
    private string _rigctldExecutable = "rigctld";
    private string _rigctldArgumentsTemplate = "-m {rigNum} -T {host} -t {port}{serialArg}";
    private string _rigctldAdditionalArguments = string.Empty;
    private string _rigctldHost = "127.0.0.1";
    private int _rigctldPort = 4532;
    private int _rigctldReconnectIntervalSeconds = 3;
    private string _selectedSerialPort = string.Empty;
    private string _riglistFilePath = string.Empty;
    private string _statusMessage = string.Empty;
    private string _configFilePath = string.Empty;
    private string _newProfileName = string.Empty;

    private const int DefaultRigctldPort = 4532;

    public ConfigurationViewModel()
        : this(new HamBusLog.Hardware.SerialPortCatalogService())
    {
    }

    internal ConfigurationViewModel(HamBusLog.Hardware.ISerialPortCatalogService serialPortCatalogService)
    {
        _serialPortCatalogService = serialPortCatalogService;
        AvailableProfiles = new ObservableCollection<string>();
        AvailableSerialPorts = new ObservableCollection<string>();
        AvailableRigRadioOptions = new ObservableCollection<RigRadioOption>();
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
    public ObservableCollection<RigRadioOption> AvailableRigRadioOptions { get; }

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

    public Color MenuBackgroundColor
    {
        get => _menuBackgroundColor;
        set => SetProperty(ref _menuBackgroundColor, value);
    }

    public Color MenuForegroundColor
    {
        get => _menuForegroundColor;
        set => SetProperty(ref _menuForegroundColor, value);
    }

    public Color ButtonNormalColor
    {
        get => _buttonNormalColor;
        set => SetProperty(ref _buttonNormalColor, value);
    }

    public Color ButtonNormalForegroundColor
    {
        get => _buttonNormalForegroundColor;
        set => SetProperty(ref _buttonNormalForegroundColor, value);
    }

    public Color ButtonCautionColor
    {
        get => _buttonCautionColor;
        set => SetProperty(ref _buttonCautionColor, value);
    }

    public Color ButtonCautionForegroundColor
    {
        get => _buttonCautionForegroundColor;
        set => SetProperty(ref _buttonCautionForegroundColor, value);
    }

    public Color ButtonDangerColor
    {
        get => _buttonDangerColor;
        set => SetProperty(ref _buttonDangerColor, value);
    }

    public Color ButtonDangerForegroundColor
    {
        get => _buttonDangerForegroundColor;
        set => SetProperty(ref _buttonDangerForegroundColor, value);
    }

    public Color ButtonForegroundColor
    {
        get => _buttonForegroundColor;
        set => SetProperty(ref _buttonForegroundColor, value);
    }

    public Color InputBackgroundColor
    {
        get => _inputBackgroundColor;
        set => SetProperty(ref _inputBackgroundColor, value);
    }

    public Color InputForegroundColor
    {
        get => _inputForegroundColor;
        set => SetProperty(ref _inputForegroundColor, value);
    }

    public Color InputBorderColor
    {
        get => _inputBorderColor;
        set => SetProperty(ref _inputBorderColor, value);
    }

    public Color InputSelectionBackgroundColor
    {
        get => _inputSelectionBackgroundColor;
        set => SetProperty(ref _inputSelectionBackgroundColor, value);
    }

    public Color InputSelectionForegroundColor
    {
        get => _inputSelectionForegroundColor;
        set => SetProperty(ref _inputSelectionForegroundColor, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public string DatabaseFolderPath
    {
        get => _databaseFolderPath;
        set
        {
            if (SetProperty(ref _databaseFolderPath, value ?? string.Empty))
                OnPropertyChanged(nameof(DatabaseFilePath));
        }
    }

    public string DatabaseFileName
    {
        get => _databaseFileName;
        set
        {
            if (SetProperty(ref _databaseFileName, value ?? string.Empty))
                OnPropertyChanged(nameof(DatabaseFilePath));
        }
    }

    public string DatabaseFilePath
    {
        get => BuildDatabasePath(DatabaseFolderPath, DatabaseFileName);
        set
        {
            var (folderPath, fileName) = SplitDatabasePath(value);
            DatabaseFolderPath = folderPath;
            DatabaseFileName = fileName;
        }
    }

    public string AdifDirectory
    {
        get => _adifDirectory;
        set => SetProperty(ref _adifDirectory, value ?? string.Empty);
    }

    public string SelectedRigRadioTag
    {
        get => _selectedRigRadioTag;
        set
        {
            var nextTag = value ?? "radio-1";
            if (string.Equals(nextTag, _selectedRigRadioTag, StringComparison.OrdinalIgnoreCase))
                return;

            // Preserve current editor values on the previously selected radio before switching.
            PersistRigRadioSettings(_selectedRigRadioTag);

            if (!SetProperty(ref _selectedRigRadioTag, nextTag))
                return;

            LoadSelectedRigRadioSettings();
            ApplyEditorSettingsToRigCatalog();
        }
    }

    public RigRadioOption? SelectedRigRadio
    {
        get => AvailableRigRadioOptions.FirstOrDefault(x => string.Equals(x.TagName, SelectedRigRadioTag, StringComparison.OrdinalIgnoreCase));
        set
        {
            var tag = value?.TagName ?? "radio-1";
            if (string.Equals(tag, SelectedRigRadioTag, StringComparison.OrdinalIgnoreCase))
                return;
            SelectedRigRadioTag = tag;
            OnPropertyChanged(nameof(SelectedRigRadio));
        }
    }

    public string RigctldRadioName
    {
        get => _rigctldRadioName;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? SelectedRigRadioTag
                : value.Trim();
            if (!SetProperty(ref _rigctldRadioName, normalized))
                return;

            var selectedOption = AvailableRigRadioOptions
                .FirstOrDefault(x => string.Equals(x.TagName, SelectedRigRadioTag, StringComparison.OrdinalIgnoreCase));
            if (selectedOption is not null)
                selectedOption.DisplayName = normalized;
        }
    }

    public string RigctldExecutable
    {
        get => _rigctldExecutable;
        set => SetProperty(ref _rigctldExecutable, value ?? string.Empty);
    }

    public string RigctldArgumentsTemplate
    {
        get => _rigctldArgumentsTemplate;
        set => SetProperty(ref _rigctldArgumentsTemplate, value ?? string.Empty);
    }

    public string RigctldAdditionalArguments
    {
        get => _rigctldAdditionalArguments;
        set => SetProperty(ref _rigctldAdditionalArguments, value ?? string.Empty);
    }

    public string RigctldHost
    {
        get => _rigctldHost;
        set => SetProperty(ref _rigctldHost, value ?? string.Empty);
    }

    public int RigctldPort
    {
        get => _rigctldPort;
        set => SetProperty(ref _rigctldPort, value);
    }

    public int RigctldReconnectIntervalSeconds
    {
        get => _rigctldReconnectIntervalSeconds;
        set => SetProperty(ref _rigctldReconnectIntervalSeconds, value <= 0 ? 3 : Math.Min(value, 300));
    }

    public string SelectedSerialPort
    {
        get => _selectedSerialPort;
        set
        {
            if (!SetProperty(ref _selectedSerialPort, value ?? string.Empty))
                return;

            EnsureSelectedSerialPortInList(_selectedSerialPort);
        }
    }

    public string RiglistFilePath
    {
        get => _riglistFilePath;
        set => SetProperty(ref _riglistFilePath, value ?? string.Empty);
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
            // Ensure the currently edited radio fields are written back before profile serialization.
            PersistRigRadioSettings(SelectedRigRadioTag);

            var normalizedDatabaseFolderPath = NormalizeDatabaseFolderPath(DatabaseFolderPath);
            var normalizedDatabaseFileName = NormalizeDatabaseFileName(DatabaseFileName);
            var normalizedDatabaseFilePath = BuildDatabasePath(normalizedDatabaseFolderPath, normalizedDatabaseFileName);
            var resolvedConnectionString = BuildConnectionString(ConnectionString, normalizedDatabaseFilePath);
            var existingRigctld = AppConfigurationStore.GetRigctld(_appConfig);

            var profile = new ConfigProfile
            {
                Name = _selectedProfile,
                BackgroundColor = ToHexRgb(BackgroundColor),
                ForegroundColor = ToHexRgb(ForegroundColor),
                AdifDirectory = AdifDirectory.Trim(),
                DatabaseFolderPath = normalizedDatabaseFolderPath,
                DatabaseFileName = normalizedDatabaseFileName,
                DatabaseFilePath = normalizedDatabaseFilePath,
                MenuBackgroundColor = ToHexRgb(MenuBackgroundColor),
                MenuForegroundColor = ToHexRgb(MenuForegroundColor),
                ButtonNormalColor = ToHexRgb(ButtonNormalColor),
                ButtonNormalForegroundColor = ToHexRgb(ButtonNormalForegroundColor),
                ButtonCautionColor = ToHexRgb(ButtonCautionColor),
                ButtonCautionForegroundColor = ToHexRgb(ButtonCautionForegroundColor),
                ButtonDangerColor = ToHexRgb(ButtonDangerColor),
                ButtonDangerForegroundColor = ToHexRgb(ButtonDangerForegroundColor),
                ButtonForegroundColor = ToHexRgb(ButtonNormalForegroundColor),
                InputBackgroundColor = ToHexRgb(InputBackgroundColor),
                InputForegroundColor = ToHexRgb(InputForegroundColor),
                InputBorderColor = ToHexRgb(InputBorderColor),
                InputSelectionBackgroundColor = ToHexRgb(InputSelectionBackgroundColor),
                InputSelectionForegroundColor = ToHexRgb(InputSelectionForegroundColor),
                ConnectionString = resolvedConnectionString,
                Rigctld = existingRigctld
            };

            _appConfig.Profiles[_selectedProfile] = profile;
            _appConfig.ActiveProfile = _selectedProfile;
            var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
            rigctld.ReconnectIntervalSeconds = RigctldReconnectIntervalSeconds <= 0 ? 3 : Math.Min(RigctldReconnectIntervalSeconds, 300);
            rigctld.ActiveRadioTag = SelectedRigRadioTag;
            var radio = AppConfigurationStore.GetRigctldRadio(rigctld, SelectedRigRadioTag);
            radio.DisplayName = string.IsNullOrWhiteSpace(RigctldRadioName)
                ? radio.TagName
                : RigctldRadioName.Trim();
            radio.Executable = RigctldExecutable.Trim();
            radio.ArgumentsTemplate = RigctldArgumentsTemplate.Trim();
            radio.AdditionalArguments = RigctldAdditionalArguments.Trim();
            radio.Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim();
            radio.Port = RigctldPort <= 0 ? DefaultRigctldPort : RigctldPort;
            radio.SerialPortName = SelectedSerialPort.Trim();
            radio.RiglistFilePath = RiglistFilePath.Trim();
            var portConflictMessage = BuildPortConflictMessage(rigctld);
            if (!string.IsNullOrWhiteSpace(portConflictMessage))
            {
                StatusMessage = $"✗ Save failed: {portConflictMessage}";
                return;
            }

            RigctldPort = radio.Port;
            if (_activeRigRadioTags.Count == 0)
                _activeRigRadioTags.Add(radio.TagName);
            rigctld.ActiveRadioTags = _activeRigRadioTags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var rigRadio in rigctld.Radios)
                rigRadio.IsActive = rigctld.ActiveRadioTags.Contains(rigRadio.TagName, StringComparer.OrdinalIgnoreCase);
            if (rigctld.ActiveRadioTags.Count == 0)
                rigctld.ActiveRadioTags.Add(radio.TagName);
            rigctld.ActiveRadioTag = rigctld.ActiveRadioTags[0];
            DatabaseFolderPath = normalizedDatabaseFolderPath;
            DatabaseFileName = normalizedDatabaseFileName;
            ConnectionString = resolvedConnectionString;
            AppConfigurationStore.Save(_appConfig);
            PopulateAvailableRigRadios(rigctld);
            _activeRigRadioTags = rigctld.ActiveRadioTags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (_activeRigRadioTags.Count == 0)
                _activeRigRadioTags.Add(rigctld.ActiveRadioTag);
            LoadSelectedRigRadioSettings();
            RigCatalog.ReloadFromConfiguration();
            App.ApplyThemeFromProfile(profile);
            _ = App.RigctldConnectionManager.RefreshActiveConnectionsAsync();
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
            AdifDirectory = src.AdifDirectory,
            DatabaseFolderPath = src.DatabaseFolderPath,
            DatabaseFileName = src.DatabaseFileName,
            DatabaseFilePath = src.DatabaseFilePath,
            MenuBackgroundColor = ToHexRgb(MenuBackgroundColor),
            MenuForegroundColor = ToHexRgb(MenuForegroundColor),
            ButtonNormalColor = ToHexRgb(ButtonNormalColor),
            ButtonNormalForegroundColor = ToHexRgb(ButtonNormalForegroundColor),
            ButtonCautionColor = ToHexRgb(ButtonCautionColor),
            ButtonCautionForegroundColor = ToHexRgb(ButtonCautionForegroundColor),
            ButtonDangerColor = ToHexRgb(ButtonDangerColor),
            ButtonDangerForegroundColor = ToHexRgb(ButtonDangerForegroundColor),
            ButtonForegroundColor = ToHexRgb(ButtonNormalForegroundColor),
            InputBackgroundColor = ToHexRgb(InputBackgroundColor),
            InputForegroundColor = ToHexRgb(InputForegroundColor),
            InputBorderColor = ToHexRgb(InputBorderColor),
            InputSelectionBackgroundColor = ToHexRgb(InputSelectionBackgroundColor),
            InputSelectionForegroundColor = ToHexRgb(InputSelectionForegroundColor),
            ConnectionString = src.ConnectionString
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

        try { MenuBackgroundColor = Color.Parse(profile.MenuBackgroundColor); }
        catch { MenuBackgroundColor = Color.Parse("#111827"); }

        try { MenuForegroundColor = Color.Parse(profile.MenuForegroundColor); }
        catch { MenuForegroundColor = Color.Parse("#FFFFFF"); }

        try { ButtonNormalColor = Color.Parse(profile.ButtonNormalColor); }
        catch { ButtonNormalColor = Color.Parse("#2563EB"); }

        try { ButtonNormalForegroundColor = Color.Parse(string.IsNullOrWhiteSpace(profile.ButtonNormalForegroundColor) ? profile.ButtonForegroundColor : profile.ButtonNormalForegroundColor); }
        catch { ButtonNormalForegroundColor = Color.Parse("#FFFFFF"); }

        try { ButtonCautionColor = Color.Parse(profile.ButtonCautionColor); }
        catch { ButtonCautionColor = Color.Parse("#D97706"); }

        try { ButtonCautionForegroundColor = Color.Parse(string.IsNullOrWhiteSpace(profile.ButtonCautionForegroundColor) ? profile.ButtonForegroundColor : profile.ButtonCautionForegroundColor); }
        catch { ButtonCautionForegroundColor = Color.Parse("#FFFFFF"); }

        try { ButtonDangerColor = Color.Parse(profile.ButtonDangerColor); }
        catch { ButtonDangerColor = Color.Parse("#DC2626"); }

        try { ButtonDangerForegroundColor = Color.Parse(string.IsNullOrWhiteSpace(profile.ButtonDangerForegroundColor) ? profile.ButtonForegroundColor : profile.ButtonDangerForegroundColor); }
        catch { ButtonDangerForegroundColor = Color.Parse("#FFFFFF"); }

        try { ButtonForegroundColor = Color.Parse(string.IsNullOrWhiteSpace(profile.ButtonForegroundColor) ? profile.ButtonNormalForegroundColor : profile.ButtonForegroundColor); }
        catch { ButtonForegroundColor = ButtonNormalForegroundColor; }

        try { InputBackgroundColor = Color.Parse(profile.InputBackgroundColor); }
        catch { InputBackgroundColor = Color.Parse("#2C3E50"); }

        try { InputForegroundColor = Color.Parse(profile.InputForegroundColor); }
        catch { InputForegroundColor = Color.Parse("#FFFFFF"); }

        try { InputBorderColor = Color.Parse(profile.InputBorderColor); }
        catch { InputBorderColor = Color.Parse("#34495E"); }

        try { InputSelectionBackgroundColor = Color.Parse(profile.InputSelectionBackgroundColor); }
        catch { InputSelectionBackgroundColor = Color.Parse("#2C3E50"); }

        try { InputSelectionForegroundColor = Color.Parse(profile.InputSelectionForegroundColor); }
        catch { InputSelectionForegroundColor = Color.Parse("#FFFFFF"); }

        ConnectionString = profile.ConnectionString;
        var configuredDatabasePath = !string.IsNullOrWhiteSpace(profile.DatabaseFilePath)
            ? profile.DatabaseFilePath
            : ExtractDatabaseFilePathFromConnectionString(profile.ConnectionString);

        DatabaseFolderPath = !string.IsNullOrWhiteSpace(profile.DatabaseFolderPath)
            ? profile.DatabaseFolderPath
            : ExtractDatabaseFolderFromPath(configuredDatabasePath);

        DatabaseFileName = !string.IsNullOrWhiteSpace(profile.DatabaseFileName)
            ? profile.DatabaseFileName
            : ExtractDatabaseFileNameFromPath(configuredDatabasePath);

        if (string.IsNullOrWhiteSpace(DatabaseFileName))
            DatabaseFileName = "hambuslog.db";

        AdifDirectory = profile.AdifDirectory;
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        RigctldReconnectIntervalSeconds = rigctld.ReconnectIntervalSeconds <= 0 ? 3 : Math.Min(rigctld.ReconnectIntervalSeconds, 300);
        PopulateAvailableRigRadios(rigctld);
        _activeRigRadioTags = rigctld.ActiveRadioTags
            .Concat(rigctld.Radios.Where(x => x.IsActive).Select(x => x.TagName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (_activeRigRadioTags.Count == 0)
            _activeRigRadioTags.Add(rigctld.ActiveRadioTag);
        SelectedRigRadioTag = rigctld.ActiveRadioTag;
        LoadSelectedRigRadioSettings();
        RefreshSerialPorts();
        App.ApplyThemeFromProfile(profile);
    }

    public void RefreshSerialPorts()
    {
        var discovered = _serialPortCatalogService.GetAvailablePorts();
        var currentSelection = SelectedSerialPort;

        AvailableSerialPorts.Clear();
        foreach (var port in discovered)
            AvailableSerialPorts.Add(port);

        EnsureSelectedSerialPortInList(currentSelection);

        OnPropertyChanged(nameof(AvailableSerialPorts));
    }

    private void Load()
    {
        _appConfig = AppConfigurationStore.Load();

        if (string.IsNullOrWhiteSpace(_appConfig.ActiveProfile))
            _appConfig.ActiveProfile = "default";

        if (_appConfig.Profiles.Count == 0)
            _appConfig.Profiles["default"] = new ConfigProfile { Name = "default" };

        if (!_appConfig.Profiles.ContainsKey(_appConfig.ActiveProfile))
            _appConfig.Profiles[_appConfig.ActiveProfile] = new ConfigProfile { Name = _appConfig.ActiveProfile };

        AvailableProfiles.Clear();
        foreach (var key in _appConfig.Profiles.Keys)
            AvailableProfiles.Add(key);

        _selectedProfile = _appConfig.ActiveProfile;
        if (!AvailableProfiles.Contains(_selectedProfile))
            AvailableProfiles.Add(_selectedProfile);

        OnPropertyChanged(nameof(SelectedProfile));
        LoadProfile(_selectedProfile);
        ConfigFilePath = AppConfigurationStore.GetConfigFilePath();
    }

    private List<string> _activeRigRadioTags = [];

    private void PopulateAvailableRigRadios(RigctldConfiguration rigctld)
    {
        var activeTags = rigctld.ActiveRadioTags
            .Concat(rigctld.Radios.Where(x => x.IsActive).Select(x => x.TagName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        AvailableRigRadioOptions.Clear();
        foreach (var radio in rigctld.Radios
                     .OrderBy(x => x.RadioId <= 0 ? 1 : x.RadioId)
                     .ThenBy(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.TagName : x.DisplayName, StringComparer.OrdinalIgnoreCase))
            AvailableRigRadioOptions.Add(new RigRadioOption(radio.TagName, radio.DisplayName, activeTags.Contains(radio.TagName)));
        OnPropertyChanged(nameof(AvailableRigRadioOptions));
        OnPropertyChanged(nameof(SelectedRigRadio));
    }

    private void LoadSelectedRigRadioSettings()
    {
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        var radio = AppConfigurationStore.GetRigctldRadio(rigctld, SelectedRigRadioTag);
        rigctld.ActiveRadioTag = radio.TagName;
        SelectedRigRadioTag = radio.TagName;
        RigctldRadioName = string.IsNullOrWhiteSpace(radio.DisplayName) ? radio.TagName : radio.DisplayName;
        RigctldExecutable = radio.Executable ?? string.Empty;
        RigctldArgumentsTemplate = radio.ArgumentsTemplate ?? string.Empty;
        RigctldAdditionalArguments = radio.AdditionalArguments ?? string.Empty;
        RigctldHost = string.IsNullOrWhiteSpace(radio.Host) ? "127.0.0.1" : radio.Host;
        RigctldPort = radio.Port <= 0 ? 4532 : radio.Port;
        SelectedSerialPort = radio.SerialPortName;
        RiglistFilePath = radio.RiglistFilePath;
        EnsureSelectedSerialPortInList(SelectedSerialPort);
        OnPropertyChanged(nameof(SelectedRigRadio));
    }

    private void EnsureSelectedSerialPortInList(string? serialPort)
    {
        if (string.IsNullOrWhiteSpace(serialPort))
            return;

        if (AvailableSerialPorts.Contains(serialPort))
            return;

        AvailableSerialPorts.Insert(0, serialPort);
        OnPropertyChanged(nameof(AvailableSerialPorts));
    }

    public IReadOnlyList<string> GetActiveRigRadioTags() => _activeRigRadioTags.ToList();

    public void SetActiveRigRadioTags(IEnumerable<string>? tags)
    {
        var selected = (tags ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selected.Count == 0)
            selected.Add(SelectedRigRadioTag);

        _activeRigRadioTags = selected;
        var activeSet = selected.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in AvailableRigRadioOptions)
            option.IsActive = activeSet.Contains(option.TagName);
        OnPropertyChanged(nameof(AvailableRigRadioOptions));
    }

    public void AddRigRadio()
    {
        PersistRigRadioSettings(SelectedRigRadioTag);
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);

        var nextId = rigctld.Radios.Count == 0 ? 1 : rigctld.Radios.Max(x => x.RadioId) + 1;
        var tag = $"radio-{nextId}";
        while (rigctld.Radios.Any(x => string.Equals(x.TagName, tag, StringComparison.OrdinalIgnoreCase)))
        {
            nextId++;
            tag = $"radio-{nextId}";
        }

        var newRadio = new RigRadioConfig
        {
            RadioId = nextId,
            TagName = tag,
            DisplayName = tag,
            Executable = "rigctld",
            ArgumentsTemplate = "-m {rigNum} -T {host} -t {port}{serialArg}",
            AdditionalArguments = string.Empty,
            Host = string.IsNullOrWhiteSpace(rigctld.Host) ? "127.0.0.1" : rigctld.Host,
            Port = GetNextAvailableRigPort(rigctld, null),
            SerialPortName = rigctld.SerialPortName,
            RiglistFilePath = rigctld.RiglistFilePath,
            IsActive = false
        };

        rigctld.Radios.Add(newRadio);
        PopulateAvailableRigRadios(rigctld);
        SelectedRigRadioTag = newRadio.TagName;
        OnPropertyChanged(nameof(SelectedRigRadio));
        StatusMessage = $"Added {newRadio.DisplayName}.";
    }

    public void RemoveSelectedRigRadio()
    {
        PersistRigRadioSettings(SelectedRigRadioTag);
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        if (rigctld.Radios.Count <= 1)
        {
            StatusMessage = "At least one radio must remain configured.";
            return;
        }

        var selected = rigctld.Radios.FirstOrDefault(x => string.Equals(x.TagName, SelectedRigRadioTag, StringComparison.OrdinalIgnoreCase));
        if (selected is null)
            return;

        rigctld.Radios.Remove(selected);
        rigctld.ActiveRadioTags = rigctld.ActiveRadioTags
            .Where(x => !string.Equals(x, selected.TagName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _activeRigRadioTags = _activeRigRadioTags
            .Where(x => !string.Equals(x, selected.TagName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (_activeRigRadioTags.Count == 0 && rigctld.Radios.Count > 0)
            _activeRigRadioTags.Add(rigctld.Radios[0].TagName);

        rigctld.ActiveRadioTag = _activeRigRadioTags[0];
        PopulateAvailableRigRadios(rigctld);
        SelectedRigRadioTag = rigctld.ActiveRadioTag;
        StatusMessage = $"Removed radio '{selected.DisplayName}'.";
    }

    private static string ToHexRgb(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static string NormalizeDatabaseFolderPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return Path.GetFullPath(path.Trim());
    }

    private static string NormalizeDatabaseFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "hambuslog.db";

        var trimmed = fileName.Trim();
        var normalized = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(normalized) ? "hambuslog.db" : normalized;
    }

    private static string BuildDatabasePath(string? folderPath, string? fileName)
    {
        var normalizedFileName = NormalizeDatabaseFileName(fileName);
        if (string.IsNullOrWhiteSpace(folderPath))
            return normalizedFileName;

        return Path.Combine(folderPath.Trim(), normalizedFileName);
    }

    private static (string FolderPath, string FileName) SplitDatabasePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (string.Empty, "hambuslog.db");

        var trimmed = path.Trim();
        var fileName = Path.GetFileName(trimmed);
        var directory = Path.GetDirectoryName(trimmed) ?? string.Empty;

        return (directory, string.IsNullOrWhiteSpace(fileName) ? "hambuslog.db" : fileName);
    }

    private static string BuildConnectionString(string? currentConnectionString, string normalizedDatabaseFilePath)
    {
        if (!string.IsNullOrWhiteSpace(normalizedDatabaseFilePath))
            return $"Data Source={normalizedDatabaseFilePath}";

        return string.IsNullOrWhiteSpace(currentConnectionString)
            ? "Data Source=hambuslog.db"
            : currentConnectionString.Trim();
    }

    private static string ExtractDatabaseFilePathFromConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        var match = Regex.Match(connectionString, @"(?:^|;)\s*Data\s+Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return string.Empty;

        var rawPath = match.Groups[1].Value.Trim().Trim('\'', '"');
        if (string.IsNullOrWhiteSpace(rawPath))
            return string.Empty;

        return rawPath;
    }

    private static string ExtractDatabaseFolderFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return Path.GetDirectoryName(path) ?? string.Empty;
    }

    private static string ExtractDatabaseFileNameFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return Path.GetFileName(path);
    }

    private void PersistRigRadioSettings(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return;

        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        var radio = AppConfigurationStore.GetRigctldRadio(rigctld, tagName);
        radio.DisplayName = string.IsNullOrWhiteSpace(RigctldRadioName)
            ? radio.TagName
            : RigctldRadioName.Trim();
        radio.Executable = RigctldExecutable.Trim();
        radio.ArgumentsTemplate = RigctldArgumentsTemplate.Trim();
        radio.AdditionalArguments = RigctldAdditionalArguments.Trim();
        radio.Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim();
        radio.Port = RigctldPort <= 0 ? DefaultRigctldPort : RigctldPort;
        radio.SerialPortName = SelectedSerialPort.Trim();
        radio.RiglistFilePath = RiglistFilePath.Trim();
    }

    public void CommitSelectedRigRadioEdits()
    {
        PersistRigRadioSettings(SelectedRigRadioTag);
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        var portConflictMessage = BuildPortConflictMessage(rigctld);
        if (!string.IsNullOrWhiteSpace(portConflictMessage))
        {
            StatusMessage = $"✗ Save failed: {portConflictMessage}";
            return;
        }

        var selectedRadio = AppConfigurationStore.GetRigctldRadio(rigctld, SelectedRigRadioTag);
        RigctldPort = selectedRadio.Port;
        ApplyEditorSettingsToRigCatalog();
        PopulateAvailableRigRadios(rigctld);
        OnPropertyChanged(nameof(SelectedRigRadio));

        // Persist radio edits directly from the editor dialog so display name changes are not lost.
        AppConfigurationStore.Save(_appConfig);

        StatusMessage = $"Saved radio '{selectedRadio.DisplayName}'.";
    }

    private static string BuildPortConflictMessage(RigctldConfiguration rigctld)
    {
        var conflicts = rigctld.Radios
            .Where(x => x.Port > 0)
            .GroupBy(x => x.Port)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key)
            .ToList();

        if (conflicts.Count == 0)
            return string.Empty;

        var messages = conflicts.Select(group =>
        {
            var radios = group
                .Select(radio => string.IsNullOrWhiteSpace(radio.DisplayName) ? radio.TagName : radio.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return $"Port {group.Key} is used by {string.Join(", ", radios)}";
        });

        return string.Join("; ", messages);
    }

    public void RevertSelectedRigRadioEdits()
    {
        LoadSelectedRigRadioSettings();
        ApplyEditorSettingsToRigCatalog();
    }

    private void ApplyEditorSettingsToRigCatalog()
    {
        RigCatalog.RigctldExecutable = RigctldExecutable;
        RigCatalog.RigctldArgumentsTemplate = RigctldArgumentsTemplate;
        RigCatalog.RigctldAdditionalArguments = RigctldAdditionalArguments;
        RigCatalog.RigctldHost = RigctldHost;
        RigCatalog.RigctldPort = RigctldPort;
        RigCatalog.SelectedSerialPort = SelectedSerialPort;

        var configuredPath = RiglistFilePath.Trim();
        if (!string.IsNullOrWhiteSpace(configuredPath)
            && !string.Equals(configuredPath, RigCatalog.FilePath, StringComparison.Ordinal))
            RigCatalog.LoadFromFile(configuredPath);
    }

    private static int GetNextAvailableRigPort(RigctldConfiguration rigctld, string? excludeTag)
    {
        var usedPorts = rigctld.Radios
            .Where(x => x.Port > 0 && (string.IsNullOrWhiteSpace(excludeTag)
                || !string.Equals(x.TagName, excludeTag, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Port)
            .ToHashSet();

        return FindFirstAvailablePort(usedPorts);
    }

    private static List<(string TagName, int FromPort, int ToPort)> EnsureUniqueRigPorts(RigctldConfiguration rigctld, string? priorityTag)
    {
        var orderedRadios = rigctld.Radios
            .OrderBy(x => string.Equals(x.TagName, priorityTag, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(x => x.RadioId <= 0 ? int.MaxValue : x.RadioId)
            .ThenBy(x => x.TagName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var corrections = new List<(string TagName, int FromPort, int ToPort)>();
        var usedPorts = new HashSet<int>();
        foreach (var radio in orderedRadios)
        {
            var original = radio.Port <= 0 ? DefaultRigctldPort : radio.Port;
            var candidate = original;
            while (usedPorts.Contains(candidate) && candidate < 65535)
                candidate++;

            if (usedPorts.Contains(candidate))
                candidate = FindFirstAvailablePort(usedPorts);

            radio.Port = candidate;
            usedPorts.Add(candidate);

            if (candidate != original)
                corrections.Add((radio.TagName, original, candidate));
        }

        return corrections;
    }

    private static string BuildPortCorrectionMessage(IReadOnlyCollection<(string TagName, int FromPort, int ToPort)> corrections)
    {
        if (corrections.Count == 0)
            return string.Empty;

        var items = corrections
            .Select(x => $"{x.TagName}: {x.FromPort}->{x.ToPort}")
            .ToList();
        return $"Port conflict resolved ({string.Join(", ", items)})";
    }

    private static int FindFirstAvailablePort(HashSet<int> usedPorts)
    {
        for (var port = DefaultRigctldPort; port <= 65535; port++)
        {
            if (!usedPorts.Contains(port))
                return port;
        }

        for (var port = 1; port < DefaultRigctldPort; port++)
        {
            if (!usedPorts.Contains(port))
                return port;
        }

        return DefaultRigctldPort;
    }

    public void Dispose()
    {
        RigCatalog.Dispose();
    }
}

public sealed class RigRadioOption : ObservableObject
{
    private bool _isActive;
    private string _displayName;

    public RigRadioOption(string tagName, string? displayName, bool isActive = false)
    {
        TagName = tagName;
        _displayName = string.IsNullOrWhiteSpace(displayName) ? tagName : displayName.Trim();
        _isActive = isActive;
    }

    public string TagName { get; }
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetProperty(ref _displayName, string.IsNullOrWhiteSpace(value) ? TagName : value.Trim()))
                OnPropertyChanged(nameof(Display));
        }
    }

    public string Display => DisplayName;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public override string ToString() => Display;
}

