namespace HamBusLog;

public partial class App
{
    public static RigCatalogStore RigCatalogStore { get; } = new();
    
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
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void ApplyThemeFromActiveProfile()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        ApplyThemeFromProfile(profile);
    }

    public static void ApplyThemeFromProfile(ConfigProfile profile)
    {
        if (Current?.Resources is not ResourceDictionary resources)
            return;

        var background = ParseColor(profile.BackgroundColor, Color.Parse("#1F2937"));
        var foreground = ParseColor(profile.ForegroundColor, Color.Parse("#FFFFFF"));
        var menuBackground = ParseColor(profile.MenuBackgroundColor, Color.Parse("#111827"));
        var menuForeground = ParseColor(profile.MenuForegroundColor, foreground);
        var buttonNormal = ParseColor(profile.ButtonNormalColor, Color.Parse("#2563EB"));
        var buttonCaution = ParseColor(profile.ButtonCautionColor, Color.Parse("#D97706"));
        var buttonDanger = ParseColor(profile.ButtonDangerColor, Color.Parse("#DC2626"));
        var buttonForeground = ParseColor(profile.ButtonForegroundColor, Color.Parse("#FFFFFF"));

        SetBrush(resources, "AppWindowBackgroundBrush", background);
        SetBrush(resources, "AppHeaderBackgroundBrush", background);
        SetBrush(resources, "AppPanelBackgroundBrush", AdjustBrightness(background, 0.08));
        SetBrush(resources, "AppMenuBackgroundBrush", menuBackground);
        SetBrush(resources, "AppMenuForegroundBrush", menuForeground);
        SetBrush(resources, "AppForegroundBrush", foreground);
        SetBrush(resources, "AppMutedForegroundBrush", AdjustBrightness(foreground, -0.35));
        SetBrush(resources, "AppBorderBrush", AdjustBrightness(background, 0.16));
        SetBrush(resources, "AppAccentBrush", Color.Parse("#3498DB"));
        SetBrush(resources, "AppButtonNormalBrush", buttonNormal);
        SetBrush(resources, "AppButtonCautionBrush", buttonCaution);
        SetBrush(resources, "AppButtonDangerBrush", buttonDanger);
        SetBrush(resources, "AppButtonForegroundBrush", buttonForeground);
        SetBrush(resources, "AppErrorBrush", Color.Parse("#FF6B6B"));
        SetBrush(resources, "AppWarningBrush", Color.Parse("#FFD700"));
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
