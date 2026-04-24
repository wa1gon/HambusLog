namespace HamBusLog;

public partial class App
{
    public static RigCatalogStore RigCatalogStore { get; } = new();

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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

        SetBrush(resources, "AppWindowBackgroundBrush", background);
        SetBrush(resources, "AppHeaderBackgroundBrush", background);
        SetBrush(resources, "AppPanelBackgroundBrush", AdjustBrightness(background, 0.08));
        SetBrush(resources, "AppForegroundBrush", foreground);
        SetBrush(resources, "AppMutedForegroundBrush", AdjustBrightness(foreground, -0.35));
        SetBrush(resources, "AppBorderBrush", AdjustBrightness(background, 0.16));
        SetBrush(resources, "AppAccentBrush", Color.Parse("#3498DB"));
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





