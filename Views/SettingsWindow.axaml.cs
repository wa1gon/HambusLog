namespace HamBusLog.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private ColorPicker? _bgPicker;
    private ColorPicker? _fgPicker;
    private TextBlock? _bgHex;
    private TextBlock? _fgHex;

    public SettingsWindow()
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel();
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
        if (e.PropertyName is nameof(SettingsViewModel.BackgroundColor))
            SyncPickerColor(_bgPicker, _bgHex, _viewModel.BackgroundColor);
        if (e.PropertyName is nameof(SettingsViewModel.ForegroundColor))
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
    public void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

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
}
