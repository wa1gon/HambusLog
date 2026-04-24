namespace HamBusLog.Views;

public partial class ConfigurationWindow
{
    private readonly ConfigurationViewModel _viewModel;
    private ColorPicker? _bgPicker;
    private ColorPicker? _fgPicker;
    private ColorPicker? _menuBgPicker;
    private ColorPicker? _menuFgPicker;
    private ColorPicker? _btnNormalPicker;
    private ColorPicker? _btnCautionPicker;
    private ColorPicker? _btnDangerPicker;
    private ColorPicker? _btnFgPicker;
    private TextBlock? _bgHex;
    private TextBlock? _fgHex;
    private TextBlock? _menuBgHex;
    private TextBlock? _menuFgHex;
    private TextBlock? _btnNormalHex;
    private TextBlock? _btnCautionHex;
    private TextBlock? _btnDangerHex;
    private TextBlock? _btnFgHex;
    private TextBlock? _menuContrastLabel;
    private TextBlock? _buttonNormalContrastLabel;
    private TextBlock? _buttonCautionContrastLabel;
    private TextBlock? _buttonDangerContrastLabel;

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
        _menuBgPicker = this.FindControl<ColorPicker>("MenuBgColorPicker");
        _menuFgPicker = this.FindControl<ColorPicker>("MenuFgColorPicker");
        _btnNormalPicker = this.FindControl<ColorPicker>("BtnNormalColorPicker");
        _btnCautionPicker = this.FindControl<ColorPicker>("BtnCautionColorPicker");
        _btnDangerPicker = this.FindControl<ColorPicker>("BtnDangerColorPicker");
        _btnFgPicker = this.FindControl<ColorPicker>("BtnFgColorPicker");
        _bgHex    = this.FindControl<TextBlock>("BgColorHex");
        _fgHex    = this.FindControl<TextBlock>("FgColorHex");
        _menuBgHex = this.FindControl<TextBlock>("MenuBgColorHex");
        _menuFgHex = this.FindControl<TextBlock>("MenuFgColorHex");
        _btnNormalHex = this.FindControl<TextBlock>("BtnNormalColorHex");
        _btnCautionHex = this.FindControl<TextBlock>("BtnCautionColorHex");
        _btnDangerHex = this.FindControl<TextBlock>("BtnDangerColorHex");
        _btnFgHex = this.FindControl<TextBlock>("BtnFgColorHex");
        _menuContrastLabel = this.FindControl<TextBlock>("MenuContrastLabel");
        _buttonNormalContrastLabel = this.FindControl<TextBlock>("ButtonNormalContrastLabel");
        _buttonCautionContrastLabel = this.FindControl<TextBlock>("ButtonCautionContrastLabel");
        _buttonDangerContrastLabel = this.FindControl<TextBlock>("ButtonDangerContrastLabel");

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
                UpdateContrastLabels();
            };

        if (_menuBgPicker != null)
            _menuBgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.MenuBackgroundColor = ev.NewColor;
                UpdateHex(_menuBgHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_menuFgPicker != null)
            _menuFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.MenuForegroundColor = ev.NewColor;
                UpdateHex(_menuFgHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnNormalPicker != null)
            _btnNormalPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonNormalColor = ev.NewColor;
                UpdateHex(_btnNormalHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnCautionPicker != null)
            _btnCautionPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonCautionColor = ev.NewColor;
                UpdateHex(_btnCautionHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnDangerPicker != null)
            _btnDangerPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonDangerColor = ev.NewColor;
                UpdateHex(_btnDangerHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnFgPicker != null)
            _btnFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonForegroundColor = ev.NewColor;
                UpdateHex(_btnFgHex, ev.NewColor);
                UpdateContrastLabels();
            };

        UpdateContrastLabels();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var needsContrastRefresh = false;
        if (e.PropertyName is nameof(ConfigurationViewModel.BackgroundColor))
            SyncPickerColor(_bgPicker, _bgHex, _viewModel.BackgroundColor);
        if (e.PropertyName is nameof(ConfigurationViewModel.ForegroundColor))
        {
            SyncPickerColor(_fgPicker, _fgHex, _viewModel.ForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.MenuBackgroundColor))
        {
            SyncPickerColor(_menuBgPicker, _menuBgHex, _viewModel.MenuBackgroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.MenuForegroundColor))
        {
            SyncPickerColor(_menuFgPicker, _menuFgHex, _viewModel.MenuForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonNormalColor))
        {
            SyncPickerColor(_btnNormalPicker, _btnNormalHex, _viewModel.ButtonNormalColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonCautionColor))
        {
            SyncPickerColor(_btnCautionPicker, _btnCautionHex, _viewModel.ButtonCautionColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonDangerColor))
        {
            SyncPickerColor(_btnDangerPicker, _btnDangerHex, _viewModel.ButtonDangerColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonForegroundColor))
        {
            SyncPickerColor(_btnFgPicker, _btnFgHex, _viewModel.ButtonForegroundColor);
            needsContrastRefresh = true;
        }

        if (needsContrastRefresh)
            UpdateContrastLabels();
    }

    private void SyncPickersFromViewModel()
    {
        SyncPickerColor(_bgPicker, _bgHex, _viewModel.BackgroundColor);
        SyncPickerColor(_fgPicker, _fgHex, _viewModel.ForegroundColor);
        SyncPickerColor(_menuBgPicker, _menuBgHex, _viewModel.MenuBackgroundColor);
        SyncPickerColor(_menuFgPicker, _menuFgHex, _viewModel.MenuForegroundColor);
        SyncPickerColor(_btnNormalPicker, _btnNormalHex, _viewModel.ButtonNormalColor);
        SyncPickerColor(_btnCautionPicker, _btnCautionHex, _viewModel.ButtonCautionColor);
        SyncPickerColor(_btnDangerPicker, _btnDangerHex, _viewModel.ButtonDangerColor);
        SyncPickerColor(_btnFgPicker, _btnFgHex, _viewModel.ButtonForegroundColor);
        UpdateContrastLabels();
    }

    private void UpdateContrastLabels()
    {
        SetContrastLabel(_menuContrastLabel, "Menu contrast", _viewModel.MenuForegroundColor, _viewModel.MenuBackgroundColor);
        SetContrastLabel(_buttonNormalContrastLabel, "Button normal", _viewModel.ButtonForegroundColor, _viewModel.ButtonNormalColor);
        SetContrastLabel(_buttonCautionContrastLabel, "Button caution", _viewModel.ButtonForegroundColor, _viewModel.ButtonCautionColor);
        SetContrastLabel(_buttonDangerContrastLabel, "Button danger", _viewModel.ButtonForegroundColor, _viewModel.ButtonDangerColor);
    }

    private static void SetContrastLabel(TextBlock? label, string title, Color foreground, Color background)
    {
        if (label is null)
            return;

        var ratio = GetContrastRatio(foreground, background);
        var grade = ratio >= 7.0 ? "AAA" : ratio >= 4.5 ? "AA" : ratio >= 3.0 ? "Large text only" : "Fail";
        label.Text = $"{title}: {ratio:0.00}:1 ({grade})";
    }

    private static double GetContrastRatio(Color a, Color b)
    {
        var l1 = RelativeLuminance(a);
        var l2 = RelativeLuminance(b);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        static double ToLinear(byte c)
        {
            var s = c / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        var r = ToLinear(color.R);
        var g = ToLinear(color.G);
        var b = ToLinear(color.B);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
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






