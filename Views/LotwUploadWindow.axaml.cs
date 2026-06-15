namespace HamBusLog.Views;

public partial class LotwUploadWindow : Window
{
    private readonly LotwUploadViewModel _viewModel;

    public LotwUploadWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(LotwUploadWindow));
        App.Toasts.RegisterWindow(this);
        _viewModel = new LotwUploadViewModel();
        DataContext = _viewModel;
    }

    public async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var config = AppConfigurationStore.Load();
            var profile = AppConfigurationStore.GetActiveProfile(config);
            var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
            var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Download LoTW ADIF",
                SuggestedFileName = $"lotw-download-{DateTime.UtcNow:yyyyMMdd-HHmmss}.adi",
                SuggestedStartLocation = suggestedStartLocation,
                FileTypeChoices =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType("ADIF files") { Patterns = ["*.adi", "*.adif"] }
                ]
            });

            if (file is null)
                return;

            var path = file.Path.LocalPath;
            var progressWindow = new OperationProgressWindow("Downloading LoTW ADIF", $"Saving {Path.GetFileName(path)}...");
            progressWindow.Show(this);

            try
            {
                await _viewModel.DownloadAdifAsync(path);
                if (_viewModel.StatusMessage.StartsWith("✓", StringComparison.Ordinal))
                    App.Toasts.ShowSuccess("LoTW download", _viewModel.StatusMessage.TrimStart('✓').Trim());
                else if (_viewModel.StatusMessage.StartsWith("✗", StringComparison.Ordinal))
                    App.Toasts.ShowError("LoTW download failed", _viewModel.StatusMessage.TrimStart('✗').Trim());
            }
            finally
            {
                progressWindow.Close();
            }
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("LoTW download error", ex.Message);
        }
    }

    public async void OnUploadDateRangeClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.UploadDateRangeAsync();
            if (_viewModel.StatusMessage.StartsWith("✓", StringComparison.Ordinal))
                App.Toasts.ShowSuccess("LoTW upload", _viewModel.StatusMessage.TrimStart('✓').Trim());
            else if (_viewModel.StatusMessage.StartsWith("✗", StringComparison.Ordinal))
                App.Toasts.ShowError("LoTW upload failed", _viewModel.StatusMessage.TrimStart('✗').Trim());
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("LoTW upload error", ex.Message);
        }
    }

    public async void OnUploadAllClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.UploadAllAsync();
            if (_viewModel.StatusMessage.StartsWith("✓", StringComparison.Ordinal))
                App.Toasts.ShowSuccess("LoTW upload", _viewModel.StatusMessage.TrimStart('✓').Trim());
            else if (_viewModel.StatusMessage.StartsWith("✗", StringComparison.Ordinal))
                App.Toasts.ShowError("LoTW upload failed", _viewModel.StatusMessage.TrimStart('✗').Trim());
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("LoTW upload error", ex.Message);
        }
    }

    public void OnSaveSettingsClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.SaveSettings();
        App.Toasts.ShowSuccess("LoTW settings", "Settings saved.");
    }

    public void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

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


