namespace HamBusLog.Views;

public partial class CabrilloExportWindow : Window
{
    public CabrilloExportWindow()
    {
        InitializeComponent();
        DataContext = new CabrilloExportViewModel();
    }

    private CabrilloExportViewModel ViewModel => (CabrilloExportViewModel)DataContext!;

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        var contest = ViewModel.SelectedContest;
        if (contest is null)
        {
            App.Toasts.ShowWarning("Cabrillo export", "Select a contest to export.");
            return;
        }

        if (!contest.IsSupported)
        {
            App.Toasts.ShowWarning("Cabrillo export", "That contest is not supported yet.");
            return;
        }

        // For Field Day contests, prompt for bonus points
        int bonusPoints = 0;
        if (IsFieldDayContest(contest))
        {
            var bonusWindow = new FieldDayBonusPointsWindow();
            if (Owner is Window owner)
            {
                var result = await bonusWindow.ShowDialog<bool>(owner);
                if (result == true)
                    bonusPoints = bonusWindow.BonusPoints;
                else
                    return; // User cancelled
            }
            else
            {
                var result = await bonusWindow.ShowDialog<bool>(this);
                if (result == true)
                    bonusPoints = bonusWindow.BonusPoints;
                else
                    return; // User cancelled
            }
        }

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = $"Export {contest.DisplayName} Cabrillo",
            SuggestedFileName = $"hambuslog-{contest.Key.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.cab",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("Cabrillo files") { Patterns = ["*.cab"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        var progressWindow = new OperationProgressWindow("Exporting Cabrillo", $"Writing {Path.GetFileName(path)}...");
        if (Owner is Window owner2)
            progressWindow.Show(owner2);
        else
            progressWindow.Show(this);

        var settings = ViewModel.BuildSettings();
        // Create new settings with bonus points included
        settings = new CabrilloExportSettings(settings.Headers, bonusPoints);
        try
        {
            var exported = await CabrilloExportService.ExportToFileAsync(path, contest.Definition, settings);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("Cabrillo export complete", $"Exported {exported} QSO record(s)." );
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("Cabrillo export failed", ex.Message);
        }
        finally
        {
            progressWindow.Close();
        }
    }

    private static bool IsFieldDayContest(CabrilloContestOption contest)
    {
        if (contest is null)
            return false;

        var key = contest.Key.Trim().ToUpperInvariant();
        var adifId = contest.AdifContestId.Trim().ToUpperInvariant();
        var displayName = contest.DisplayName.Trim();

        return key is "ARRL-FD" or "ARRL-FIELD-DAY"
               || adifId is "ARRL-FD" or "ARRL-FIELD-DAY"
               || displayName.Contains("ARRL Field Day", StringComparison.OrdinalIgnoreCase)
               || displayName.Contains("Field Day", StringComparison.OrdinalIgnoreCase);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private static void RememberAdifDirectory(AppConfiguration config, string exportedFilePath)
    {
        var directory = Path.GetDirectoryName(exportedFilePath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        var profile = AppConfigurationStore.GetActiveProfile(config);
        if (string.Equals(profile.AdifDirectory, directory, StringComparison.Ordinal))
            return;

        profile.AdifDirectory = directory;
        AppConfigurationStore.Save(config);
    }

    private async Task<Avalonia.Platform.Storage.IStorageFolder?> TryGetFolderFromPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return null;

        var fullPath = Path.GetFullPath(path);
        if (!Path.EndsInDirectorySeparator(fullPath))
            fullPath += Path.DirectorySeparatorChar;

        return await StorageProvider.TryGetFolderFromPathAsync(new Uri(fullPath));
    }
}
