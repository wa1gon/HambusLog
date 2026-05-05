namespace HamBusLog;

public partial class App
{
    public static RigCatalogStore RigCatalogStore { get; } = new();
    public static IRigctldConnectionManager RigctldConnectionManager { get; } = new RigctldConnectionManager();
    public static IDxSpotFeed DxSpotFeed { get; } = new DxSpotFeed();
    public static IDxClusterTcpReader DxClusterReader { get; } = new DxClusterTcpReader();
    public static IToastService Toasts { get; } = new ToastService();

    private static HamBusLogDbContext? _dbContext;
    public static HamBusLogDbContext DbContext
    {
        get
        {
            if (_dbContext == null)
            {
                var connectionString = ResolveAppConnectionString();
                var dbPath = ExtractDataSourcePath(connectionString);

                if (!string.IsNullOrWhiteSpace(dbPath))
                {
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                }

                var options = HamBusLogDbContextFactory.BuildOptions(DatabaseProvider.Sqlite, connectionString);
                _dbContext = new HamBusLogDbContext(options);
                _dbContext.Database.EnsureCreated();
                System.Diagnostics.Debug.WriteLine($"Database context created: {connectionString}");
            }
            return _dbContext;
        }
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
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            ApplyThemeFromActiveProfile();
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            RigCatalogStore.InitializeFromConfiguration();
            _ = RigctldConnectionManager.RefreshActiveConnectionsAsync();
            _ = DxClusterReader.StartAsync();
            desktop.Exit += (_, _) =>
            {
                RigctldConnectionManager.Dispose();
                DxClusterReader.Dispose();
            };
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            Toasts.RegisterWindow(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
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
}
