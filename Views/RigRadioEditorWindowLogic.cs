namespace HamBusLog.Views;

public partial class RigRadioEditorWindow
{
    private readonly ConfigurationViewModel _viewModel;

    public RigRadioEditorWindow()
        : this(new ConfigurationViewModel())
    {
    }

    public RigRadioEditorWindow(ConfigurationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void OnRefreshSerialPortsClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.RefreshSerialPorts();
    }

    public async void OnBrowseRiglistClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select rigctld rig list file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("Text files") { Patterns = ["*.txt", "*.lst", "*.log", "*"] }
            ]
        });

        if (files.Count > 0)
        {
            _viewModel.RiglistFilePath = files[0].Path.LocalPath;
            OnLoadRiglistClicked(sender, e);
        }
    }

    public void OnLoadRiglistClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.RiglistFilePath))
        {
            _viewModel.RigCatalog.SetStatusMessage("Rig list path is empty.");
            return;
        }

        _viewModel.RigCatalog.LoadFromFile(_viewModel.RiglistFilePath);
    }

    public void OnCatalogReloadClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.RigCatalog.FilePath))
            _viewModel.RigCatalog.Reload();
    }

    public void OnCatalogSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        var selected = grid.SelectedItems?.Cast<RigCatalogEntry>() ?? [];
        _viewModel.RigCatalog.SetSelectedEntries(selected);
    }

    public async void OnCatalogCopyCommandClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.RigCatalog.RigctldCommandLine))
        {
            _viewModel.RigCatalog.SetStatusMessage("No rigctld command to copy.");
            return;
        }

        var topLevel = GetTopLevel(this);
        var clipboard = topLevel?.Clipboard;
        if (clipboard is null)
        {
            _viewModel.RigCatalog.SetStatusMessage("Clipboard is not available in this environment.");
            return;
        }

        try
        {
            await clipboard.SetTextAsync(_viewModel.RigCatalog.RigctldCommandLine);
            _viewModel.RigCatalog.SetStatusMessage("rigctld command copied to clipboard.");
        }
        catch (Exception ex)
        {
            _viewModel.RigCatalog.SetStatusMessage($"Failed to copy command: {ex.Message}");
        }
    }

    public void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.CommitSelectedRigRadioEdits();
        Close();
    }

    public void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.RevertSelectedRigRadioEdits();
        Close();
    }
}


