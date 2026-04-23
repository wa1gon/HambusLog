namespace HamBusLog.Views;

public partial class RigCatalogWindow : Window
{
    private readonly RigCatalogViewModel _viewModel;

    public RigCatalogWindow()
    {
        InitializeComponent();
        _viewModel = new RigCatalogViewModel();
        DataContext = _viewModel;
    }

    public async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select rigctld rig list file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("Text / List files") { Patterns = ["*.txt", "*.lst", "*.log", "*"] }
            ]
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            _viewModel.LoadFromFile(path);
        }
    }

    public void OnReloadClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.FilePath))
            _viewModel.Reload();
    }

    public async void OnCopyCommandClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.RigctldCommandLine))
        {
            _viewModel.SetStatusMessage("✗ No rigctld command to copy.");
            return;
        }

        var topLevel = GetTopLevel(this);
        var clipboard = topLevel?.Clipboard;
        if (clipboard is null)
        {
            _viewModel.SetStatusMessage("✗ Clipboard is not available in this environment.");
            return;
        }

        try
        {
            await clipboard.SetTextAsync(_viewModel.RigctldCommandLine);
            _viewModel.SetStatusMessage("✓ rigctld command copied to clipboard.");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatusMessage($"✗ Failed to copy command: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}



