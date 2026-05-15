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

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = $"Export {contest.DisplayName} Cabrillo",
            SuggestedFileName = $"hambuslog-{contest.Key.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("Cabrillo files") { Patterns = ["*.log", "*.cbr"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        var progressWindow = new OperationProgressWindow("Exporting Cabrillo", $"Writing {Path.GetFileName(path)}...");
        if (Owner is Window owner)
            progressWindow.Show(owner);
        else
            progressWindow.Show(this);

        var settings = ViewModel.BuildSettings();
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
