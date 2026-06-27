namespace HamBusLog;

public partial class App
{
    public static RigCatalogStore RigCatalogStore { get; } = new();
    public static IRigctldConnectionManager RigctldConnectionManager { get; } = new RigctldConnectionManager();
    public static IDxSpotFeed DxSpotFeed { get; } = new DxSpotFeed();
    public static IDxClusterTcpReader DxClusterReader { get; } = new DxClusterTcpReader();
    public static IDxClusterSpotPublisher DxClusterSpotPublisher { get; } = new DxClusterSpotPublisher();
    public static IWsjtBridgeService WsjtBridgeService { get; } = new WsjtBridgeService();
    public static ILogTypeSelectionService LogTypeSelectionService { get; } = new LogTypeSelectionService();
    public static IDigitalVoiceKeyerService DigitalVoiceKeyerService { get; } = new DigitalVoiceKeyerService();
    public static IToastService Toasts { get; } = new ToastService();

    private static HamBusLogDbContext? _dbContext;
    private static string _dbConnectionString = string.Empty;
    private static readonly object _dbContextSync = new();
    private static readonly object _dxClusterLogSync = new();
    public static event EventHandler? DbContextReinitialized;
    public static event EventHandler<Qso>? QsoSaved;

    public static HamBusLogDbContext DbContext
    {
        get
        {
            if (_dbContext == null)
            {
                lock (_dbContextSync)
                {
                    if (_dbContext == null)
                        _dbContext = CreateDbContext(ResolveAppConnectionString());
                }
            }
            return _dbContext;
        }
    }

    public static bool ReinitializeDbContext(string? requestedConnectionString, out string errorMessage)
    {
        try
        {
            var nextConnectionString = string.IsNullOrWhiteSpace(requestedConnectionString)
                ? ResolveAppConnectionString()
                : requestedConnectionString.Trim();

            lock (_dbContextSync)
            {
                if (_dbContext is not null && string.Equals(_dbConnectionString, nextConnectionString, StringComparison.Ordinal))
                {
                    errorMessage = string.Empty;
                    return false;
                }

                var nextContext = CreateDbContext(nextConnectionString);
                var previous = _dbContext;
                _dbContext = nextContext;

                try
                {
                    previous?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DbContext dispose warning: {ex.Message}");
                }
            }

            DbContextReinitialized?.Invoke(null, EventArgs.Empty);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static void RaiseQsoSaved(Qso qso)
    {
        if (qso is null)
            return;

        QsoSaved?.Invoke(null, qso);
        _ = TryAutoUploadToLotwAsync(qso);
    }

    private static async Task TryAutoUploadToLotwAsync(Qso qso)
    {
        try
        {
            var config = AppConfigurationStore.Load();
            if (!config.Lotw.Enabled || !config.Lotw.AutoUploadOnLog)
                return;

            var service = new LotwUploadService(config.Lotw, AppConfigurationStore.GetActiveProfile(config));
            if (!service.IsConfigured)
                return;

            var result = await service.UploadQsosAsync([qso]);
            if (result.Success)
                Toasts.ShowSuccess("LoTW", $"Uploaded {qso.Call} to LoTW.");
            else
                Toasts.ShowWarning("LoTW upload failed", result.Message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoTW auto-upload error: {ex.Message}");
            Toasts.ShowWarning("LoTW auto-upload error", ex.Message);
        }
    }

    private static HamBusLogDbContext CreateDbContext(string connectionString)
    {
        var dbPath = ExtractDataSourcePath(connectionString);

        if (!string.IsNullOrWhiteSpace(dbPath))
        {
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        var options = HamBusLogDbContextFactory.BuildOptions(DatabaseProvider.Sqlite, connectionString);
        var context = new HamBusLogDbContext(options);
        context.Database.EnsureCreated();
        _dbConnectionString = connectionString;
        System.Diagnostics.Debug.WriteLine($"Database context created: {connectionString}");
        return context;
    }

    private static string ResolveAppConnectionString()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);

        if (!string.IsNullOrWhiteSpace(profile.ConnectionString))
            return profile.ConnectionString.Trim();

        // Default for both Windows and Linux: user home under HamBusLog.
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultDbPath = Path.Combine(homeDir, "HamBusLog", "hambuslog.db");
        return $"Data Source={defaultDbPath}";
    }

    private static string ExtractDataSourcePath(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        var match = Regex.Match(connectionString, @"(?:^|;)\s*Data\s+Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return string.Empty;

        return match.Groups[1].Value.Trim().Trim('\'', '"');
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Keep alive during splash before the main window exists.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ApplyThemeFromActiveProfile();
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            RigCatalogStore.InitializeFromConfiguration();
            ClearDxClusterLogs();
            LogDxClusterNonSpot("SYS", "Application started");
            _ = RigctldConnectionManager.RefreshActiveConnectionsAsync();
            _ = DxClusterReader.StartAsync();
            _ = WsjtBridgeService.StartAsync();
            WsjtBridgeService.LoggedQsoReceived += OnWsjtLoggedQsoReceived;
            desktop.Exit += (_, _) =>
            {
                WsjtBridgeService.LoggedQsoReceived -= OnWsjtLoggedQsoReceived;
                RigctldConnectionManager.Dispose();
                DxClusterReader.Dispose();
                WsjtBridgeService.Dispose();
            };

            var splash = new SplashWindow();
            App.TrackWindowPlacement(splash, nameof(SplashWindow));

            // When splash closes: open the main window on the same monitor the splash was on.
            splash.Closed += (_, _) =>
            {
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                // Place the main window on the same monitor as splash by reusing splash top-left.
                var targetPosition = splash.LastKnownPosition;
                mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                mainWindow.Position = targetPosition;

                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
                // Some WMs may override initial placement; enforce once after mapping.
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    mainWindow.Position = targetPosition;
                });
                mainWindow.Activate();

                if (AppConfigurationStore.ConsumeContestRepairNotice())
                {
                    Toasts.ShowInfo(
                        "Configuration updated",
                        "Restored missing built-in contest fields (including FD section/class). Review Configuration if needed.");
                }
            };

            splash.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async void OnWsjtLoggedQsoReceived(object? sender, WsjtLoggedQso loggedQso)
    {
        await SaveWsjtLoggedQsoAsync(loggedQso);
    }

    private static Task SaveWsjtLoggedQsoAsync(WsjtLoggedQso loggedQso)
    {
        if (loggedQso is null || string.IsNullOrWhiteSpace(loggedQso.Call))
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            try
            {
                var contest = LogTypeSelectionService.GetSelectedContestDefinition();
                var qso = BuildQsoFromWsjt(loggedQso, contest);

                using var context = CreateTransientDbContext();
                context.Qsos.Add(qso);
                context.SaveChanges();

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    RaiseQsoSaved(qso);
                    Toasts.ShowSuccess("WSJT-X", $"Auto-logged {qso.Call} on {qso.Band} {qso.Mode} ({contest.DisplayName}).");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WSJT-X auto-log failed: {ex.Message}");
                Avalonia.Threading.Dispatcher.UIThread.Post(() => Toasts.ShowError("WSJT-X auto-log failed", ex.Message));
            }
        });
    }

    private static HamBusLogDbContext CreateTransientDbContext()
    {
        string connectionString;
        lock (_dbContextSync)
            connectionString = string.IsNullOrWhiteSpace(_dbConnectionString) ? ResolveAppConnectionString() : _dbConnectionString;

        var options = HamBusLogDbContextFactory.BuildOptions(DatabaseProvider.Sqlite, connectionString);
        var context = new HamBusLogDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Qso BuildQsoFromWsjt(WsjtLoggedQso loggedQso, ContestDefinition contest)
    {
        var mode = !string.IsNullOrWhiteSpace(loggedQso.Submode)
            ? loggedQso.Submode.Trim().ToUpperInvariant()
            : loggedQso.Mode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(mode))
            mode = "DIGITAL";

        var band = loggedQso.Band.Trim().ToUpperInvariant();
        var freq = ParseFrequencyMhz(loggedQso.FreqMhz);
        if (string.IsNullOrWhiteSpace(band))
            band = DeriveBandFromMhz(freq) ?? "20M";

        var stationCall = loggedQso.StationCallsign.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(stationCall))
        {
            var config = AppConfigurationStore.Load();
            stationCall = AppConfigurationStore.GetActiveProfile(config).StationCallSign.Trim().ToUpperInvariant();
        }

        var qso = new Qso
        {
            Call = loggedQso.Call.Trim().ToUpperInvariant(),
            StationCallSign = stationCall,
            QsoDate = (loggedQso.TimeOnUtc ?? DateTimeOffset.UtcNow).UtcDateTime,
            Band = band,
            Mode = mode,
            ContestId = contest.AdifContestId,
            Freq = freq,
            Country = loggedQso.Country.Trim().ToUpperInvariant(),
            State = loggedQso.State.Trim().ToUpperInvariant(),
            RstSent = loggedQso.RstSent.Trim(),
            RstRcvd = loggedQso.RstRcvd.Trim(),
            Details = new List<QsoDetail>()
        };

        AddDetailIfPresent(qso.Details, "GRID", loggedQso.GridSquare);
        AddDetailIfPresent(qso.Details, "MY_GRIDSQUARE", loggedQso.MyGridSquare);
        AddDetailIfPresent(qso.Details, "COUNTY", loggedQso.County);
        AddDetailIfPresent(qso.Details, "NAME", loggedQso.Name);
        AddDetailIfPresent(qso.Details, "OPERATOR", loggedQso.Operator);
        AddDetailIfPresent(qso.Details, "EXCHANGE", loggedQso.ExchangeReceived);

        return qso;
    }

    private static decimal ParseFrequencyMhz(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : 0m;
    }

    private static string? DeriveBandFromMhz(decimal mhz)
    {
        return mhz switch
        {
            >= 1.8m and <= 2.0m => "160M",
            >= 3.5m and <= 4.0m => "80M",
            >= 5.3305m and <= 5.4065m => "60M",
            >= 7.0m and <= 7.3m => "40M",
            >= 10.1m and <= 10.15m => "30M",
            >= 14.0m and <= 14.35m => "20M",
            >= 18.068m and <= 18.168m => "17M",
            >= 21.0m and <= 21.45m => "15M",
            >= 24.89m and <= 24.99m => "12M",
            >= 28.0m and <= 29.7m => "10M",
            >= 50.0m and <= 54.0m => "6M",
            >= 144.0m and <= 148.0m => "2M",
            >= 420.0m and <= 450.0m => "70CM",
            _ => null
        };
    }

    private static void AddDetailIfPresent(ICollection<QsoDetail> details, string fieldName, string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        details.Add(new QsoDetail
        {
            FieldName = fieldName,
            FieldValue = normalized.ToUpperInvariant()
        });
    }

    public static void ApplyThemeFromActiveProfile()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        ApplyThemeFromProfile(profile);
    }

    public static void TrackWindowPlacement(Window window, string placementKey)
    {
        if (window is null || string.IsNullOrWhiteSpace(placementKey))
            return;

        void OnOpened(object? sender, EventArgs e)
        {
            try
            {
                RestoreWindowPlacement(window, placementKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RestoreWindowPlacement warning ({placementKey}): {ex.Message}");
            }
        }

        void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            try
            {
                SaveWindowPlacement(window, placementKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveWindowPlacement warning ({placementKey}): {ex.Message}");
            }
        }
        void OnClosed(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;
            window.Closing -= OnClosing;
            window.Closed -= OnClosed;
        }

        window.Opened += OnOpened;
        window.Closing += OnClosing;
        window.Closed += OnClosed;
    }

    public static void RestoreWindowPlacement(Window window, string placementKey)
    {
        if (window is null || string.IsNullOrWhiteSpace(placementKey))
            return;

        var config = AppConfigurationStore.Load();
        if (!config.WindowPlacements.TryGetValue(placementKey, out var placement))
            return;

        var target = new PixelPoint(placement.X, placement.Y);
        if (!IsPlacementOnScreen(window, target))
            return;

        window.Position = target;
    }

    public static void SaveWindowPlacement(Window window, string placementKey)
    {
        if (window is null || string.IsNullOrWhiteSpace(placementKey))
            return;

        try
        {
            var config = AppConfigurationStore.Load();
            config.WindowPlacements[placementKey] = new WindowPlacement
            {
                X = window.Position.X,
                Y = window.Position.Y
            };

            AppConfigurationStore.Save(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveWindowPlacement warning ({placementKey}): {ex.Message}");
        }
    }

    public static TWindow? FindOpenWindow<TWindow>()
        where TWindow : Window
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        return desktop.Windows.OfType<TWindow>().FirstOrDefault();
    }

    public static bool ActivateOpenWindow<TWindow>()
        where TWindow : Window
    {
        var window = FindOpenWindow<TWindow>();
        if (window is null)
            return false;

        if (!window.IsVisible)
            window.Show();

        window.Activate();
        return true;
    }

    public static void ApplyThemeFromProfile(ConfigProfile profile)
    {
        if (Current?.Resources is not ResourceDictionary resources)
            return;

        var background = ParseColor(profile.BackgroundColor, Color.Parse("#0F172A"));
        var foreground = ParseColor(profile.ForegroundColor, Color.Parse("#E5E7EB"));
        var menuBackground = ParseColor(profile.MenuBackgroundColor, Color.Parse("#111827"));
        var menuForeground = ParseColor(profile.MenuForegroundColor, Color.Parse("#F9FAFB"));
        var buttonNormal = ParseColor(profile.ButtonNormalColor, Color.Parse("#2563EB"));
        var legacyButtonForeground = ParseColor(profile.ButtonForegroundColor, Color.Parse("#FFFFFF"));
        var buttonNormalForeground = ParseColor(profile.ButtonNormalForegroundColor, legacyButtonForeground);
        var buttonCaution = ParseColor(profile.ButtonCautionColor, Color.Parse("#B45309"));
        var buttonCautionForeground = ParseColor(profile.ButtonCautionForegroundColor, legacyButtonForeground);
        var buttonDanger = ParseColor(profile.ButtonDangerColor, Color.Parse("#B91C1C"));
        var buttonDangerForeground = ParseColor(profile.ButtonDangerForegroundColor, legacyButtonForeground);
        var requestedHoverFont = ParseColor(profile.HoverFontColor, Color.Parse("#FFFFFF"));
        var hoverFontColor = EnsureReadableHoverForeground(requestedHoverFont, buttonNormal, buttonCaution, buttonDanger);
        var inputBackground = ParseColor(profile.InputBackgroundColor, Color.Parse("#1F2937"));
        var inputForeground = EnsureReadableForeground(
            ParseColor(profile.InputForegroundColor, Color.Parse("#F9FAFB")),
            inputBackground);
        var inputBorder = ParseColor(profile.InputBorderColor, Color.Parse("#334155"));
        var accent = Color.Parse("#3498DB");
        var inputSelectionBackground = ParseColor(profile.InputSelectionBackgroundColor, buttonNormal);
        if (IsVisuallyClose(inputSelectionBackground, inputBackground))
            inputSelectionBackground = buttonNormal;

        var inputSelectionForeground = EnsureReadableForeground(
            ParseColor(profile.InputSelectionForegroundColor, buttonNormalForeground),
            inputSelectionBackground);
        var baseFontSize = NormalizeFontSize(profile.AppFontSize);

        var mutedForeground = string.IsNullOrWhiteSpace(profile.MutedForegroundColor)
            ? AdjustBrightness(foreground, -0.35)
            : ParseColor(profile.MutedForegroundColor, AdjustBrightness(foreground, -0.35));

        SetBrush(resources, "AppWindowBackgroundBrush", background);
        SetBrush(resources, "AppHeaderBackgroundBrush", background);
        SetBrush(resources, "AppPanelBackgroundBrush", AdjustBrightness(background, 0.08));
        SetBrush(resources, "AppMenuBackgroundBrush", menuBackground);
        SetBrush(resources, "AppMenuForegroundBrush", menuForeground);
        SetBrush(resources, "AppForegroundBrush", foreground);
        SetBrush(resources, "AppMutedForegroundBrush", mutedForeground);
        SetBrush(resources, "nBrush", mutedForeground);
        SetBrush(resources, "AppBorderBrush", AdjustBrightness(background, 0.16));
        SetBrush(resources, "AppAccentBrush", accent);
        SetBrush(resources, "AppButtonNormalBrush", buttonNormal);
        SetBrush(resources, "AppButtonNormalForegroundBrush", buttonNormalForeground);
        SetBrush(resources, "AppButtonCautionBrush", buttonCaution);
        SetBrush(resources, "AppButtonCautionForegroundBrush", buttonCautionForeground);
        SetBrush(resources, "AppButtonDangerBrush", buttonDanger);
        SetBrush(resources, "AppButtonDangerForegroundBrush", buttonDangerForeground);
        SetBrush(resources, "AppButtonForegroundBrush", buttonNormalForeground);
        SetBrush(resources, "AppHoverFontBrush", hoverFontColor);
        SetBrush(resources, "AppErrorBrush", Color.Parse("#FF6B6B"));
        SetBrush(resources, "AppWarningBrush", Color.Parse("#FFD700"));
        SetBrush(resources, "TextControlBackground", inputBackground);
        SetBrush(resources, "TextControlBackgroundPointerOver", AdjustBrightness(inputBackground, 0.05));
        SetBrush(resources, "TextControlBackgroundFocused", inputBackground);
        SetBrush(resources, "TextControlForeground", inputForeground);
        SetBrush(resources, "TextControlForegroundPointerOver", inputForeground);
        SetBrush(resources, "TextControlForegroundFocused", inputForeground);
        SetBrush(resources, "TextControlBorderBrush", inputBorder);
        SetBrush(resources, "TextControlBorderBrushPointerOver", accent);
        SetBrush(resources, "TextControlBorderBrushFocused", accent);
        SetBrush(resources, "TextControlSelectionBrush", inputSelectionBackground);
        SetBrush(resources, "TextControlSelectionForegroundBrush", inputSelectionForeground);
        SetColor(resources, "TextControlSelectionHighlightColor", inputSelectionBackground);
        SetColor(resources, "TextControlSelectionHighlightColorWhenNotFocused", inputSelectionBackground);
        SetDouble(resources, "AppBaseFontSize", baseFontSize);
    }

    private static void SetBrush(ResourceDictionary resources, string key, Color color)
    {
        if (resources[key] is SolidColorBrush brush)
        {
            brush.Color = color;
            return;
        }

        resources[key] = new SolidColorBrush(color);
    }

    private static void SetColor(ResourceDictionary resources, string key, Color color)
    {
        resources[key] = color;
    }

    private static void SetDouble(ResourceDictionary resources, string key, double value)
    {
        resources[key] = value;
    }

    private static double NormalizeFontSize(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            return 12.0;

        return Math.Clamp(Math.Round(value, 1), 10.0, 24.0);
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : Color.Parse(value);
        }
        catch
        {
            return fallback;
        }
    }

    private static Color EnsureReadableForeground(Color foreground, Color background)
    {
        if (ContrastRatio(foreground, background) >= 4.5)
            return foreground;

        var black = Color.Parse("#000000");
        var white = Color.Parse("#FFFFFF");
        return ContrastRatio(white, background) >= ContrastRatio(black, background) ? white : black;
    }

    private static Color EnsureReadableHoverForeground(Color foreground, params Color[] backgrounds)
    {
        if (backgrounds.Length == 0)
            return foreground;

        var minimumContrast = backgrounds.Min(background => ContrastRatio(foreground, background));
        if (minimumContrast >= 4.5)
            return foreground;

        var black = Color.Parse("#000000");
        var white = Color.Parse("#FFFFFF");
        var blackMin = backgrounds.Min(background => ContrastRatio(black, background));
        var whiteMin = backgrounds.Min(background => ContrastRatio(white, background));
        return whiteMin >= blackMin ? white : black;
    }

    private static bool IsVisuallyClose(Color first, Color second)
    {
        var distance = Math.Abs(first.R - second.R)
            + Math.Abs(first.G - second.G)
            + Math.Abs(first.B - second.B);
        return distance < 72;
    }

    private static double ContrastRatio(Color first, Color second)
    {
        var lighter = Math.Max(RelativeLuminance(first), RelativeLuminance(second));
        var darker = Math.Min(RelativeLuminance(first), RelativeLuminance(second));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var normalized = value / 255.0;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R)
            + 0.7152 * Channel(color.G)
            + 0.0722 * Channel(color.B);
    }

    private static Color AdjustBrightness(Color color, double delta)
    {
        byte Adjust(byte input)
        {
            var next = input + (255 * delta);
            if (next < 0) next = 0;
            if (next > 255) next = 255;
            return (byte)next;
        }

        return Color.FromArgb(color.A, Adjust(color.R), Adjust(color.G), Adjust(color.B));
    }

    private static bool IsPlacementOnScreen(Window window, PixelPoint position)
    {
        var screens = window.Screens?.All;
        if (screens is null || screens.Count == 0)
            return true;

        foreach (var screen in screens)
        {
            var area = screen.WorkingArea;
            if (position.X >= area.X
                && position.X <= area.Right - 80
                && position.Y >= area.Y
                && position.Y <= area.Bottom - 40)
                return true;
        }

        return false;
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    public static string GetDataDirectoryPath()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var baseDir = string.IsNullOrWhiteSpace(profile.ApplicationLogFolderPath)
            ? (string.IsNullOrWhiteSpace(profile.DatabaseFolderPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "HamBusLog")
                : profile.DatabaseFolderPath)
            : profile.ApplicationLogFolderPath;

        var logDir = string.IsNullOrWhiteSpace(profile.ApplicationLogFolderPath)
            ? Path.Combine(baseDir, "logs")
            : baseDir;
        Directory.CreateDirectory(logDir);
        return logDir;
    }

    public static void LogDxClusterNonSpot(string direction, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        try
        {
            var dataDir = GetDataDirectoryPath();
            var fileName = $"dxcluster-nonspots-{DateTime.UtcNow:yyyyMMdd}.log";
            var path = Path.Combine(dataDir, fileName);
            var entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z [{direction}] {line}";
            lock (_dxClusterLogSync)
            {
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.WriteLine(entry);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DX cluster log error: {ex.Message}");
        }
    }

    private static void ClearDxClusterLogs()
    {
        try
        {
            var dataDir = GetDataDirectoryPath();
            var patterns = new[]
            {
                "dxcluster-*.log",
                "dxcluster-nonspots-*.log"
            };
            foreach (var pattern in patterns)
            {
                foreach (var file in Directory.EnumerateFiles(dataDir, pattern, SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DX cluster log cleanup error: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DX cluster log cleanup error: {ex.Message}");
        }
    }
}
