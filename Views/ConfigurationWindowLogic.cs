namespace HamBusLog.Views;

public partial class ConfigurationWindow
{
    private readonly ConfigurationViewModel _viewModel;
    private ColorPicker? _bgPicker;
    private ColorPicker? _fgPicker;
    private TextBlock? _bgHex;
    private TextBlock? _fgHex;

    public ConfigurationWindow()
    {
        InitializeComponent();
        _viewModel = new ConfigurationViewModel();
        DataContext = _viewModel;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _bgPicker = this.FindControl<ColorPicker>("BgColorPicker");
        _fgPicker = this.FindControl<ColorPicker>("FgColorPicker");
        _bgHex    = this.FindControl<TextBlock>("BgColorHex");
        _fgHex    = this.FindControl<TextBlock>("FgColorHex");

        // Push initial colors into pickers now that controls are fully rendered
        SyncPickersFromViewModel();

        // ViewModel → pickers (profile switch)
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Pickers → ViewModel
        if (_bgPicker != null)
            _bgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.BackgroundColor = ev.NewColor;
                UpdateHex(_bgHex, ev.NewColor);
            };

        if (_fgPicker != null)
            _fgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ForegroundColor = ev.NewColor;
                UpdateHex(_fgHex, ev.NewColor);
            };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConfigurationViewModel.BackgroundColor))
            SyncPickerColor(_bgPicker, _bgHex, _viewModel.BackgroundColor);
        if (e.PropertyName is nameof(ConfigurationViewModel.ForegroundColor))
            SyncPickerColor(_fgPicker, _fgHex, _viewModel.ForegroundColor);
    }

    private void SyncPickersFromViewModel()
    {
        SyncPickerColor(_bgPicker, _bgHex, _viewModel.BackgroundColor);
        SyncPickerColor(_fgPicker, _fgHex, _viewModel.ForegroundColor);
    }

    private static void SyncPickerColor(ColorPicker? picker, TextBlock? hex, Color color)
    {
        if (picker != null) picker.Color = color;
        UpdateHex(hex, color);
    }

    private static void UpdateHex(TextBlock? label, Color c)
    {
        if (label != null)
            label.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    public void OnSaveClicked(object? sender, RoutedEventArgs e) => _viewModel.Save();
    public void OnCloneProfileClicked(object? sender, RoutedEventArgs e) => _viewModel.CloneProfile();
    public void OnRefreshSerialPortsClicked(object? sender, RoutedEventArgs e) => _viewModel.RefreshSerialPorts();
    public void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    public async void OnCatalogBrowseClicked(object? sender, RoutedEventArgs e)
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
            _viewModel.RigCatalog.LoadFromFile(path);
        }
    }

    public void OnCatalogReloadClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.RigCatalog.FilePath))
            _viewModel.RigCatalog.Reload();
    }

    public void OnCatalogRefreshSerialPortsClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.RigCatalog.RefreshSerialPorts();
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
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}






