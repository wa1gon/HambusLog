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
    private Color _inputSelectionBackgroundColor = Color.Parse("#2563EB");
    private Color _inputSelectionForegroundColor = Color.Parse("#FFFFFF");
    private Color _mutedForegroundColor = Color.Parse("#9CA3AF");
    private string _adifDirectory = string.Empty;
    private string _databaseFolderPath = string.Empty;
    private string _databaseFileName = "hambuslog.db";
    private string _connectionString = "Data Source=hambuslog.db";
    private string _myCall = string.Empty;
    private string _myLocation = string.Empty;
    private string _myGridSquare = string.Empty;
    private string _myLatitude = string.Empty;
    private string _myLongitude = string.Empty;
    private string _myItuZone = string.Empty;
    private string _myCqZone = string.Empty;
    private string _myFieldDaySection = string.Empty;
    private string _myFieldDayClass = string.Empty;
    private int? _selectedRigRadioId;
    private string _selectedRigRadioName = string.Empty;
    private string _rigctldRadioName = string.Empty;
    private string _rigctldExecutable = "rigctld";
    private string _rigctldArgumentsTemplate = "-m {rigNum} -T {host} -t {port}{serialArg}";
    private string _rigctldAdditionalArguments = string.Empty;
    private string _rigctldHost = "127.0.0.1";
    private int _rigctldPort = 4532;
    private int _rigctldReconnectIntervalSeconds = 3;
    private string _resourcePath = string.Empty;
    private string _riglistFilePath = string.Empty;
    private string _statusMessage = string.Empty;
    private string _configFilePath = string.Empty;
    private string _newProfileName = string.Empty;
    private string _licenseKey = string.Empty;
    private string _contestDefinitionsJson = "[]";
    private string _clusterHostname = "127.0.0.1";
    private int _clusterTcpPort = 7300;
    private string _clusterCallsign = string.Empty;
    private string _clusterPassword = string.Empty;
    private string _clusterCommand = string.Empty;
    private int _clusterQueueLength = 500;

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

    public Color MutedForegroundColor
    {
        get => _mutedForegroundColor;
        set => SetProperty(ref _mutedForegroundColor, value);
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

    public string MyCall
    {
        get => _myCall;
        set => SetProperty(ref _myCall, (value ?? string.Empty).ToUpperInvariant());
    }

    public string MyLocation
    {
        get => _myLocation;
        set => SetProperty(ref _myLocation, value ?? string.Empty);
    }

    public string MyGridSquare
    {
        get => _myGridSquare;
        set => SetProperty(ref _myGridSquare, (value ?? string.Empty).ToUpperInvariant());
    }

    public string MyLatitude
    {
        get => _myLatitude;
        set => SetProperty(ref _myLatitude, value ?? string.Empty);
    }

    public string MyLongitude
    {
        get => _myLongitude;
        set => SetProperty(ref _myLongitude, value ?? string.Empty);
    }

    public string MyItuZone
    {
        get => _myItuZone;
        set => SetProperty(ref _myItuZone, value ?? string.Empty);
    }

    public string MyCqZone
    {
        get => _myCqZone;
        set => SetProperty(ref _myCqZone, value ?? string.Empty);
    }

    public string MyFieldDaySection
    {
        get => _myFieldDaySection;
        set => SetProperty(ref _myFieldDaySection, (value ?? string.Empty).ToUpperInvariant());
    }

    public string MyFieldDayClass
    {
        get => _myFieldDayClass;
        set => SetProperty(ref _myFieldDayClass, (value ?? string.Empty).ToUpperInvariant());
    }

    public string SelectedRigRadioName
    {
        get => _selectedRigRadioName;
        set
        {
            var nextRadioName = value is null ? _selectedRigRadioName : value.Trim();
            SelectRigRadio(_selectedRigRadioId, nextRadioName, persistCurrentSelection: true);
        }
    }

    public RigRadioOption? SelectedRigRadio
    {
        get => AvailableRigRadioOptions.FirstOrDefault(x => x.RadioId == _selectedRigRadioId)
            ?? AvailableRigRadioOptions.FirstOrDefault(x => string.Equals(x.RadioName, SelectedRigRadioName, StringComparison.OrdinalIgnoreCase));
        set
        {
            if (value is null)
                return;

            var radioName = value.RadioName;
            if (value.RadioId == _selectedRigRadioId
                && string.Equals(radioName, SelectedRigRadioName, StringComparison.OrdinalIgnoreCase))
                return;

            SelectRigRadio(value.RadioId, radioName, persistCurrentSelection: true);
        }
    }

    public string RigctldRadioName
    {
        get => _rigctldRadioName;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? SelectedRigRadioName
                : value.Trim();
            SetProperty(ref _rigctldRadioName, normalized);
        }
    }

    public string RigctldExecutable
    {
        get => _rigctldExecutable;
        set
        {
            if (SetProperty(ref _rigctldExecutable, value ?? string.Empty))
                RigCatalog.RigctldExecutable = _rigctldExecutable;
        }
    }

    public string RigctldArgumentsTemplate
    {
        get => _rigctldArgumentsTemplate;
        set
        {
            if (SetProperty(ref _rigctldArgumentsTemplate, value ?? string.Empty))
                RigCatalog.RigctldArgumentsTemplate = _rigctldArgumentsTemplate;
        }
    }

    public string RigctldAdditionalArguments
    {
        get => _rigctldAdditionalArguments;
        set
        {
            if (SetProperty(ref _rigctldAdditionalArguments, value ?? string.Empty))
                RigCatalog.RigctldAdditionalArguments = _rigctldAdditionalArguments;
        }
    }

    public string RigctldHost
    {
        get => _rigctldHost;
        set
        {
            if (SetProperty(ref _rigctldHost, value ?? string.Empty))
                RigCatalog.RigctldHost = _rigctldHost;
        }
    }

    public int RigctldPort
    {
        get => _rigctldPort;
        set
        {
            if (SetProperty(ref _rigctldPort, value))
                RigCatalog.RigctldPort = _rigctldPort;
        }
    }

    public int RigctldReconnectIntervalSeconds
    {
        get => _rigctldReconnectIntervalSeconds;
        set
        {
            if (!SetProperty(ref _rigctldReconnectIntervalSeconds, value <= 0 ? 3 : Math.Min(value, 300)))
                return;

            RigCatalog.RigctldRetryCount = _rigctldReconnectIntervalSeconds;
        }
    }

    public string ResourcePath
    {
        get => _resourcePath;
        set
        {
            if (!SetProperty(ref _resourcePath, value ?? string.Empty))
                return;

            EnsureResourcePathInList(_resourcePath);
            RigCatalog.ResourcePath = _resourcePath;
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

    public string LicenseKey
    {
        get => _licenseKey;
        set => SetProperty(ref _licenseKey, value ?? string.Empty);
    }

    public string ContestDefinitionsJson
    {
        get => _contestDefinitionsJson;
        set => SetProperty(ref _contestDefinitionsJson, value ?? "[]");
    }

    public string ClusterHostname
    {
        get => _clusterHostname;
        set => SetProperty(ref _clusterHostname, value ?? string.Empty);
    }

    public int ClusterTcpPort
    {
        get => _clusterTcpPort;
        set => SetProperty(ref _clusterTcpPort, value);
    }

    public string ClusterCallsign
    {
        get => _clusterCallsign;
        set => SetProperty(ref _clusterCallsign, value ?? string.Empty);
    }

    public string ClusterPassword
    {
        get => _clusterPassword;
        set => SetProperty(ref _clusterPassword, value ?? string.Empty);
    }

    public string ClusterCommand
    {
        get => _clusterCommand;
        set => SetProperty(ref _clusterCommand, value ?? string.Empty);
    }

    public int ClusterQueueLength
    {
        get => _clusterQueueLength;
        set => SetProperty(ref _clusterQueueLength, value);
    }

    public RigCatalogViewModel RigCatalog { get; } = new();

    private Color _hoverFontColor = Color.Parse("#FFFFFF");

    public Color HoverFontColor
    {
        get => _hoverFontColor;
        set => SetProperty(ref _hoverFontColor, value);
    }

    public void Save()
    {
        try
        {
            // Ensure the currently edited radio fields are written back before profile serialization.
            PersistRigRadioSettings(_selectedRigRadioId, SelectedRigRadioName);

            var normalizedLocation = NormalizeDatabaseLocation(DatabaseFolderPath, DatabaseFileName, DatabaseFilePath);
            var normalizedDatabaseFolderPath = normalizedLocation.FolderPath;
            var normalizedDatabaseFileName = normalizedLocation.FileName;
            var normalizedDatabaseFilePath = BuildDatabasePath(normalizedDatabaseFolderPath, normalizedDatabaseFileName);
            var resolvedConnectionString = BuildConnectionString(ConnectionString, normalizedDatabaseFilePath);
            if (!TryParseContestDefinitionsJson(ContestDefinitionsJson, out var parsedContests, out var parseError))
            {
                StatusMessage = $"✗ Save failed: {parseError}";
                return;
            }

            var profile = new ConfigProfile
            {
                Name = _selectedProfile,
                BackgroundColor = ToHexRgb(BackgroundColor),
                ForegroundColor = ToHexRgb(ForegroundColor),
                AdifDirectory = AdifDirectory.Trim(),
                MyCall = MyCall.Trim(),
                MyLocation = MyLocation.Trim(),
                MyGridSquare = MyGridSquare.Trim(),
                MyLatitude = MyLatitude.Trim(),
                MyLongitude = MyLongitude.Trim(),
                MyItuZone = MyItuZone.Trim(),
                MyCqZone = MyCqZone.Trim(),
                MyFieldDaySection = MyFieldDaySection.Trim(),
                MyFieldDayClass = MyFieldDayClass.Trim(),
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
                MutedForegroundColor = ToHexRgb(MutedForegroundColor),
                ConnectionString = resolvedConnectionString
            };

            _appConfig.Profiles[_selectedProfile] = profile;
            _appConfig.ActiveProfile = _selectedProfile;
            _appConfig.LicenseKey = LicenseKey.Trim();
            _appConfig.Contests = parsedContests;
            var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
            rigctld.ReconnectIntervalSeconds = RigctldReconnectIntervalSeconds <= 0 ? 3 : Math.Min(RigctldReconnectIntervalSeconds, 300);
            var radio = ResolveRigRadio(rigctld, _selectedRigRadioId, SelectedRigRadioName);
            if (radio is null)
            {
                StatusMessage = "✗ Save failed: No radio is available to save.";
                return;
            }

            radio.RadioName = string.IsNullOrWhiteSpace(RigctldRadioName)
                ? radio.RadioName
                : RigctldRadioName.Trim();
            radio.Executable = RigctldExecutable.Trim();
            radio.ArgumentsTemplate = RigctldArgumentsTemplate.Trim();
            radio.AdditionalArguments = RigctldAdditionalArguments.Trim();
            radio.Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim();
            radio.Port = RigctldPort <= 0 ? DefaultRigctldPort : RigctldPort;
            radio.SerialPortName = ResourcePath.Trim();
            rigctld.RiglistFilePath = RiglistFilePath.Trim();
            rigctld.ActiveRadioName = radio.RadioName;
            var portConflictMessage = BuildPortConflictMessage(rigctld);
            if (!string.IsNullOrWhiteSpace(portConflictMessage))
            {
                StatusMessage = $"✗ Save failed: {portConflictMessage}";
                return;
            }

            RigctldPort = radio.Port;
            if (_activeRigRadioNames.Count == 0)
                _activeRigRadioNames.Add(radio.RadioName);
            rigctld.ActiveRadioNames = _activeRigRadioNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var rigRadio in rigctld.Radios)
                rigRadio.IsActive = rigctld.ActiveRadioNames.Contains(rigRadio.RadioName, StringComparer.OrdinalIgnoreCase);
            if (rigctld.ActiveRadioNames.Count == 0)
                rigctld.ActiveRadioNames.Add(radio.RadioName);
            rigctld.ActiveRadioName = rigctld.ActiveRadioNames[0];

            _appConfig.Cluster = new ClusterConfig
            {
                Hostname = string.IsNullOrWhiteSpace(ClusterHostname) ? "127.0.0.1" : ClusterHostname.Trim(),
                TcpPort = ClusterTcpPort <= 0 ? 7300 : ClusterTcpPort,
                Callsign = ClusterCallsign.Trim(),
                Password = ClusterPassword,
                Command = ClusterCommand.Trim(),
                QueueLength = ClusterQueueLength <= 0 ? 500 : ClusterQueueLength
            };

            DatabaseFolderPath = normalizedDatabaseFolderPath;
            DatabaseFileName = normalizedDatabaseFileName;
            ConnectionString = resolvedConnectionString;
            ContestDefinitionsJson = SerializeContestDefinitions(_appConfig.Contests);
            AppConfigurationStore.Save(_appConfig);
            PopulateAvailableRigRadios(rigctld);
            _activeRigRadioNames = rigctld.ActiveRadioNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (_activeRigRadioNames.Count == 0)
                _activeRigRadioNames.Add(rigctld.ActiveRadioName);
            LoadSelectedRigRadioSettings();
            RigCatalog.ReloadFromConfiguration();
            App.ApplyThemeFromProfile(profile);
            _ = App.RigctldConnectionManager.RefreshActiveConnectionsAsync();
            _ = App.DxClusterReader.RestartAsync();
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
            MyCall = src.MyCall,
            MyLocation = src.MyLocation,
            MyGridSquare = src.MyGridSquare,
            MyLatitude = src.MyLatitude,
            MyLongitude = src.MyLongitude,
            MyItuZone = src.MyItuZone,
            MyCqZone = src.MyCqZone,
            MyFieldDaySection = src.MyFieldDaySection,
            MyFieldDayClass = src.MyFieldDayClass,
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
            MutedForegroundColor = ToHexRgb(MutedForegroundColor),
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
        catch { InputSelectionBackgroundColor = Color.Parse("#2563EB"); }

        try { InputSelectionForegroundColor = Color.Parse(profile.InputSelectionForegroundColor); }
        catch { InputSelectionForegroundColor = Color.Parse("#FFFFFF"); }

        if (!string.IsNullOrWhiteSpace(profile.MutedForegroundColor))
        {
            try { MutedForegroundColor = Color.Parse(profile.MutedForegroundColor); }
            catch { MutedForegroundColor = AdjustBrightness(ForegroundColor, -0.35); }
        }
        else
        {
            MutedForegroundColor = AdjustBrightness(ForegroundColor, -0.35);
        }

        ConnectionString = profile.ConnectionString;
        var configuredDatabasePath = !string.IsNullOrWhiteSpace(profile.DatabaseFilePath)
            ? profile.DatabaseFilePath
            : ExtractDatabaseFilePathFromConnectionString(profile.ConnectionString);

        var normalizedLocation = NormalizeDatabaseLocation(
            profile.DatabaseFolderPath,
            profile.DatabaseFileName,
            configuredDatabasePath);

        DatabaseFolderPath = normalizedLocation.FolderPath;
        DatabaseFileName = normalizedLocation.FileName;

        AdifDirectory = profile.AdifDirectory;
        MyCall = profile.MyCall;
        MyLocation = profile.MyLocation;
        MyGridSquare = profile.MyGridSquare;
        MyLatitude = profile.MyLatitude;
        MyLongitude = profile.MyLongitude;
        MyItuZone = profile.MyItuZone;
        MyCqZone = profile.MyCqZone;
        MyFieldDaySection = profile.MyFieldDaySection;
        MyFieldDayClass = profile.MyFieldDayClass;

        var cluster = _appConfig.Cluster ?? new ClusterConfig();
        ClusterHostname = string.IsNullOrWhiteSpace(cluster.Hostname) ? "127.0.0.1" : cluster.Hostname;
        ClusterTcpPort = cluster.TcpPort <= 0 ? 7300 : cluster.TcpPort;
        ClusterCallsign = cluster.Callsign ?? string.Empty;
        ClusterPassword = cluster.Password ?? string.Empty;
        ClusterCommand = cluster.Command ?? string.Empty;
        ClusterQueueLength = cluster.QueueLength <= 0 ? 500 : cluster.QueueLength;

        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        RigctldReconnectIntervalSeconds = rigctld.ReconnectIntervalSeconds <= 0 ? 3 : Math.Min(rigctld.ReconnectIntervalSeconds, 300);
        PopulateAvailableRigRadios(rigctld);
        _activeRigRadioNames = rigctld.ActiveRadioNames
            .Concat(rigctld.Radios.Where(x => x.IsActive).Select(x => x.RadioName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (_activeRigRadioNames.Count == 0)
            _activeRigRadioNames.Add(rigctld.ActiveRadioName);
        var selectedRigRadioId = rigctld.Radios.FirstOrDefault(x => string.Equals(x.RadioName, rigctld.ActiveRadioName, StringComparison.OrdinalIgnoreCase))?.RadioId;
        SelectRigRadio(selectedRigRadioId, rigctld.ActiveRadioName, persistCurrentSelection: false);
        RefreshSerialPorts();
        App.ApplyThemeFromProfile(profile);
    }

    public void RefreshSerialPorts()
    {
        var discovered = _serialPortCatalogService.GetAvailablePorts();
        var currentSelection = ResourcePath;

        AvailableSerialPorts.Clear();
        foreach (var port in discovered)
            AvailableSerialPorts.Add(port);

        EnsureResourcePathInList(currentSelection);

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
        LicenseKey = _appConfig.LicenseKey;
        ContestDefinitionsJson = SerializeContestDefinitions(_appConfig.Contests);
        ConfigFilePath = AppConfigurationStore.GetConfigFilePath();
    }

    private static string SerializeContestDefinitions(IReadOnlyList<ContestDefinitionConfig> contests)
    {
        return JsonSerializer.Serialize(contests, new JsonSerializerOptions { WriteIndented = true });
    }

    private static bool TryParseContestDefinitionsJson(string rawJson, out List<ContestDefinitionConfig> contests, out string errorMessage)
    {
        try
        {
            contests = JsonSerializer.Deserialize<List<ContestDefinitionConfig>>(rawJson ?? string.Empty) ?? [];
            if (contests.Count == 0)
            {
                errorMessage = "Contest definition list cannot be empty.";
                return false;
            }

            foreach (var contest in contests)
            {
                if (string.IsNullOrWhiteSpace(contest.Key))
                {
                    errorMessage = "Each contest requires a non-empty Key.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(contest.AdifContestId))
                {
                    errorMessage = $"Contest '{contest.Key}' requires AdifContestId.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            contests = [];
            errorMessage = "Contest JSON is invalid: " + ex.Message;
            return false;
        }
    }

    private List<string> _activeRigRadioNames = [];

    private void PopulateAvailableRigRadios(RigctldConfiguration rigctld)
    {
        var activeTags = rigctld.ActiveRadioNames
            .Concat(rigctld.Radios.Where(x => x.IsActive).Select(x => x.RadioName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        AvailableRigRadioOptions.Clear();
        foreach (var radio in rigctld.Radios
                     .OrderBy(x => x.RadioId <= 0 ? 1 : x.RadioId)
                     .ThenBy(x => x.RadioName, StringComparer.OrdinalIgnoreCase))
            AvailableRigRadioOptions.Add(new RigRadioOption(radio.RadioId, radio.RadioName, radio.RadioName, activeTags.Contains(radio.RadioName)));
        OnPropertyChanged(nameof(AvailableRigRadioOptions));
        OnPropertyChanged(nameof(SelectedRigRadio));
    }

    private void SelectRigRadio(int? radioId, string? radioName, bool persistCurrentSelection)
    {
        var normalizedName = radioName?.Trim() ?? string.Empty;
        if (radioId == _selectedRigRadioId
            && string.Equals(normalizedName, _selectedRigRadioName, StringComparison.OrdinalIgnoreCase))
            return;

        if (persistCurrentSelection)
            PersistRigRadioSettings(_selectedRigRadioId, _selectedRigRadioName);

        UpdateSelectedRigRadioIdentity(radioId, normalizedName);
        LoadSelectedRigRadioSettings();
        ApplyEditorSettingsToRigCatalog();
    }

    private void UpdateSelectedRigRadioIdentity(int? radioId, string? radioName)
    {
        var normalizedName = radioName?.Trim() ?? string.Empty;
        var idChanged = _selectedRigRadioId != radioId;
        var nameChanged = !string.Equals(_selectedRigRadioName, normalizedName, StringComparison.OrdinalIgnoreCase);

        _selectedRigRadioId = radioId;
        _selectedRigRadioName = normalizedName;

        if (nameChanged)
            OnPropertyChanged(nameof(SelectedRigRadioName));

        if (idChanged || nameChanged)
            OnPropertyChanged(nameof(SelectedRigRadio));
    }

    private void LoadSelectedRigRadioSettings()
    {
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        var radio = ResolveRigRadio(rigctld, _selectedRigRadioId, SelectedRigRadioName)
            ?? AppConfigurationStore.GetRigctldRadio(rigctld, SelectedRigRadioName);
        rigctld.ActiveRadioName = radio.RadioName;
        UpdateSelectedRigRadioIdentity(radio.RadioId, radio.RadioName);
        RigctldRadioName = radio.RadioName;
        RigctldExecutable = radio.Executable ?? string.Empty;
        RigctldArgumentsTemplate = radio.ArgumentsTemplate ?? string.Empty;
        RigctldAdditionalArguments = radio.AdditionalArguments ?? string.Empty;
        RigctldHost = string.IsNullOrWhiteSpace(radio.Host) ? "127.0.0.1" : radio.Host;
        RigctldPort = radio.Port <= 0 ? 4532 : radio.Port;
        ResourcePath = radio.SerialPortName;
        RiglistFilePath = rigctld.RiglistFilePath;
        EnsureResourcePathInList(ResourcePath);
        OnPropertyChanged(nameof(SelectedRigRadio));
    }

    private void EnsureResourcePathInList(string? serialPort)
    {
        if (string.IsNullOrWhiteSpace(serialPort))
            return;

        if (AvailableSerialPorts.Contains(serialPort))
            return;

        AvailableSerialPorts.Insert(0, serialPort);
        OnPropertyChanged(nameof(AvailableSerialPorts));
    }

    public IReadOnlyList<string> GetActiveRigRadioNames() => _activeRigRadioNames.ToList();

    public void SetActiveRigRadioNames(IEnumerable<string>? radioNames)
    {
        var selected = (radioNames ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selected.Count == 0)
            selected.Add(SelectedRigRadioName);

        _activeRigRadioNames = selected;
        var activeSet = selected.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in AvailableRigRadioOptions)
            option.IsActive = activeSet.Contains(option.RadioName);
        OnPropertyChanged(nameof(AvailableRigRadioOptions));
    }

    public void AddRigRadio()
    {
        PersistRigRadioSettings(_selectedRigRadioId, SelectedRigRadioName);
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);

        var nextId = rigctld.Radios.Count == 0 ? 1 : rigctld.Radios.Max(x => x.RadioId) + 1;

        var newRadio = new RigRadioConfig
        {
            RadioId = nextId,
            RadioName = string.Empty,
            Executable = "rigctld",
            ArgumentsTemplate = "-m {rigNum} -T {host} -t {port}{serialArg}",
            AdditionalArguments = string.Empty,
            Host = "127.0.0.1",
            Port = GetNextAvailableRigPort(rigctld, null),
            SerialPortName = string.Empty,
            IsActive = false
        };

        rigctld.Radios.Add(newRadio);
        AppConfigurationStore.Save(_appConfig);
        PopulateAvailableRigRadios(rigctld);
        SelectRigRadio(newRadio.RadioId, newRadio.RadioName, persistCurrentSelection: false);
        StatusMessage = "Added new radio.";
    }

    public void RemoveSelectedRigRadio()
    {
        PersistRigRadioSettings(_selectedRigRadioId, SelectedRigRadioName);
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        if (rigctld.Radios.Count <= 1)
        {
            StatusMessage = "At least one radio must remain configured.";
            return;
        }

        var selected = ResolveRigRadio(rigctld, _selectedRigRadioId, SelectedRigRadioName);
        if (selected is null)
            return;

        rigctld.Radios.Remove(selected);
        rigctld.ActiveRadioNames = rigctld.ActiveRadioNames
            .Where(x => !string.Equals(x, selected.RadioName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _activeRigRadioNames = _activeRigRadioNames
            .Where(x => !string.Equals(x, selected.RadioName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (_activeRigRadioNames.Count == 0 && rigctld.Radios.Count > 0)
            _activeRigRadioNames.Add(rigctld.Radios[0].RadioName);

        rigctld.ActiveRadioName = _activeRigRadioNames[0];
        PopulateAvailableRigRadios(rigctld);
        var nextRadioId = rigctld.Radios.FirstOrDefault(x => string.Equals(x.RadioName, rigctld.ActiveRadioName, StringComparison.OrdinalIgnoreCase))?.RadioId;
        SelectRigRadio(nextRadioId, rigctld.ActiveRadioName, persistCurrentSelection: false);
        StatusMessage = $"Removed radio '{selected.RadioName}'.";
    }

    private static string ToHexRgb(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static Color AdjustBrightness(Color color, double delta)
    {
        byte Clamp(double v) => (byte)Math.Max(0, Math.Min(255, v));
        return Color.FromArgb(color.A, Clamp(color.R + 255 * delta), Clamp(color.G + 255 * delta), Clamp(color.B + 255 * delta));
    }

    private static string NormalizeDatabaseFolderPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var trimmed = path.Trim();
        if (IsLikelyDatabaseFilePath(trimmed))
        {
            var fileDirectory = Path.GetDirectoryName(trimmed);
            if (string.IsNullOrWhiteSpace(fileDirectory))
                return string.Empty;

            return Path.GetFullPath(fileDirectory);
        }

        return Path.GetFullPath(trimmed);
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

    private static (string FolderPath, string FileName) NormalizeDatabaseLocation(string? folderPath, string? fileName, string? fallbackPath)
    {
        var trimmedFolder = folderPath?.Trim() ?? string.Empty;
        var trimmedFile = fileName?.Trim() ?? string.Empty;

        if (IsLikelyDatabaseFilePath(trimmedFolder))
        {
            var extractedFile = Path.GetFileName(trimmedFolder);
            var extractedFolder = Path.GetDirectoryName(trimmedFolder);

            if (!string.IsNullOrWhiteSpace(extractedFolder))
                trimmedFolder = extractedFolder;

            if (string.IsNullOrWhiteSpace(trimmedFile)
                || string.Equals(trimmedFile, "hambuslog.db", StringComparison.OrdinalIgnoreCase)
                || trimmedFile.Contains(Path.DirectorySeparatorChar)
                || trimmedFile.Contains(Path.AltDirectorySeparatorChar))
            {
                trimmedFile = extractedFile;
            }
        }

        if (string.IsNullOrWhiteSpace(trimmedFolder) || string.IsNullOrWhiteSpace(trimmedFile))
        {
            var fallback = fallbackPath?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                if (string.IsNullOrWhiteSpace(trimmedFolder))
                    trimmedFolder = ExtractDatabaseFolderFromPath(fallback);

                if (string.IsNullOrWhiteSpace(trimmedFile) || string.Equals(trimmedFile, "hambuslog.db", StringComparison.OrdinalIgnoreCase))
                    trimmedFile = ExtractDatabaseFileNameFromPath(fallback);
            }
        }

        return (NormalizeDatabaseFolderPath(trimmedFolder), NormalizeDatabaseFileName(trimmedFile));
    }

    private static bool IsLikelyDatabaseFilePath(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var trimmed = candidate.Trim();
        var extension = Path.GetExtension(trimmed);
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return extension.Equals(".db", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sqlite", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sqlite3", StringComparison.OrdinalIgnoreCase);
    }

    private void PersistRigRadioSettings(int? radioId, string? radioName)
    {
        if (radioId is null && radioName is null)
            return;

        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        var radio = ResolveRigRadio(rigctld, radioId, radioName);
        if (radio is null)
            return;

        var previousName = radio.RadioName;
        radio.RadioName = string.IsNullOrWhiteSpace(RigctldRadioName)
            ? radio.RadioName
            : RigctldRadioName.Trim();
        radio.Executable = RigctldExecutable.Trim();
        radio.ArgumentsTemplate = RigctldArgumentsTemplate.Trim();
        radio.AdditionalArguments = RigctldAdditionalArguments.Trim();
        radio.Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim();
        radio.Port = RigctldPort <= 0 ? DefaultRigctldPort : RigctldPort;
        radio.SerialPortName = ResourcePath.Trim();
        rigctld.RiglistFilePath = RiglistFilePath.Trim();

        if (!string.Equals(previousName, radio.RadioName, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(rigctld.ActiveRadioName, previousName, StringComparison.OrdinalIgnoreCase))
                rigctld.ActiveRadioName = radio.RadioName;

            for (var i = 0; i < rigctld.ActiveRadioNames.Count; i++)
            {
                if (string.Equals(rigctld.ActiveRadioNames[i], previousName, StringComparison.OrdinalIgnoreCase))
                    rigctld.ActiveRadioNames[i] = radio.RadioName;
            }

            for (var i = 0; i < _activeRigRadioNames.Count; i++)
            {
                if (string.Equals(_activeRigRadioNames[i], previousName, StringComparison.OrdinalIgnoreCase))
                    _activeRigRadioNames[i] = radio.RadioName;
            }

            if (string.Equals(SelectedRigRadioName, previousName, StringComparison.OrdinalIgnoreCase))
                UpdateSelectedRigRadioIdentity(radio.RadioId, radio.RadioName);
        }
    }

    public bool CommitSelectedRigRadioEdits()
    {
        PersistRigRadioSettings(_selectedRigRadioId, SelectedRigRadioName);
        var rigctld = AppConfigurationStore.GetRigctld(_appConfig);
        var portConflictMessage = BuildPortConflictMessage(rigctld);
        if (!string.IsNullOrWhiteSpace(portConflictMessage))
        {
            StatusMessage = $"✗ Save failed: {portConflictMessage}";
            return false;
        }

        var selectedRadio = ResolveRigRadio(rigctld, _selectedRigRadioId, SelectedRigRadioName);
        if (selectedRadio is null)
        {
            StatusMessage = "✗ Save failed: No radio is available to save.";
            return false;
        }

        RigctldPort = selectedRadio.Port;
        ApplyEditorSettingsToRigCatalog();
        PopulateAvailableRigRadios(rigctld);
        OnPropertyChanged(nameof(SelectedRigRadio));

        // Persist radio edits directly from the editor dialog so display name changes are not lost.
        AppConfigurationStore.Save(_appConfig);

        StatusMessage = $"Saved radio '{selectedRadio.RadioName}'.";
        return true;
    }

    private static RigRadioConfig? ResolveRigRadio(RigctldConfiguration rigctld, int? radioId, string? radioName)
    {
        if (radioId is int id)
        {
            var byId = rigctld.Radios.FirstOrDefault(x => x.RadioId == id);
            if (byId is not null)
                return byId;
        }

        var requested = radioName?.Trim();
        if (requested is not null)
        {
            var byRequested = rigctld.Radios.FirstOrDefault(x =>
                string.Equals(x.RadioName, requested, StringComparison.OrdinalIgnoreCase));
            if (byRequested is not null)
                return byRequested;
        }

        var active = rigctld.ActiveRadioName?.Trim();
        if (!string.IsNullOrWhiteSpace(active))
        {
            var byActive = rigctld.Radios.FirstOrDefault(x =>
                string.Equals(x.RadioName, active, StringComparison.OrdinalIgnoreCase));
            if (byActive is not null)
                return byActive;
        }

        return rigctld.Radios.FirstOrDefault();
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
                    .Select(radio => radio.RadioName)
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
        RigCatalog.RigctldRetryCount = RigctldReconnectIntervalSeconds;
        RigCatalog.ResourcePath = ResourcePath;

        var configuredPath = RiglistFilePath.Trim();
        if (!string.IsNullOrWhiteSpace(configuredPath)
            && (!string.Equals(configuredPath, RigCatalog.FilePath, StringComparison.Ordinal)
                || RigCatalog.FilteredEntries.Count == 0))
            RigCatalog.LoadFromFile(configuredPath);
    }

    private static int GetNextAvailableRigPort(RigctldConfiguration rigctld, string? excludeRadioName)
    {
        var usedPorts = rigctld.Radios
            .Where(x => x.Port > 0 && (string.IsNullOrWhiteSpace(excludeRadioName)
                || !string.Equals(x.RadioName, excludeRadioName, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Port)
            .ToHashSet();

        return FindFirstAvailablePort(usedPorts);
    }

    private static List<(string RadioName, int FromPort, int ToPort)> EnsureUniqueRigPorts(RigctldConfiguration rigctld, string? priorityRadioName)
    {
        var orderedRadios = rigctld.Radios
            .OrderBy(x => string.Equals(x.RadioName, priorityRadioName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(x => x.RadioId <= 0 ? int.MaxValue : x.RadioId)
            .ThenBy(x => x.RadioName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var corrections = new List<(string RadioName, int FromPort, int ToPort)>();
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
                corrections.Add((radio.RadioName, original, candidate));
        }

        return corrections;
    }

    private static string BuildPortCorrectionMessage(IReadOnlyCollection<(string RadioName, int FromPort, int ToPort)> corrections)
    {
        if (corrections.Count == 0)
            return string.Empty;

        var items = corrections
            .Select(x => $"{x.RadioName}: {x.FromPort}->{x.ToPort}")
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
    private readonly int _radioId;
    private bool _isActive;
    private string _radioName;

    public RigRadioOption(int radioId, string radioName, string? displayName, bool isActive = false)
    {
        _radioId = radioId;
        _radioName = string.IsNullOrWhiteSpace(displayName) ? radioName : displayName.Trim();
        _isActive = isActive;
    }

    public int RadioId => _radioId;

    public string RadioName
    {
        get => _radioName;
        set
        {
            if (SetProperty(ref _radioName, value?.Trim() ?? string.Empty))
                OnPropertyChanged(nameof(Display));
        }
    }

    public string Display => string.IsNullOrWhiteSpace(RadioName) ? "(new radio)" : RadioName;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public override string ToString() => Display;
}
