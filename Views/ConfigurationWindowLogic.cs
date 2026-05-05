using Avalonia.Threading;

namespace HamBusLog.Views;

public partial class ConfigurationWindow
{
    private readonly ConfigurationViewModel _viewModel;
    private ColorPicker? _bgPicker;
    private ColorPicker? _fgPicker;
    private ColorPicker? _menuBgPicker;
    private ColorPicker? _menuFgPicker;
    private ColorPicker? _btnNormalPicker;
    private ColorPicker? _btnNormalFgPicker;
    private ColorPicker? _btnCautionPicker;
    private ColorPicker? _btnCautionFgPicker;
    private ColorPicker? _btnDangerPicker;
    private ColorPicker? _btnDangerFgPicker;
    private ColorPicker? _inputBgPicker;
    private ColorPicker? _inputFgPicker;
    private ColorPicker? _inputBorderPicker;
    private ColorPicker? _inputSelectionBgPicker;
    private ColorPicker? _inputSelectionFgPicker;
    private ColorPicker? _mutedFgPicker;
    private ColorPicker? _hoverFontColorPicker;
    private TextBlock? _bgHex;
    private TextBlock? _fgHex;
    private TextBlock? _menuBgHex;
    private TextBlock? _menuFgHex;
    private TextBlock? _btnNormalHex;
    private TextBlock? _btnNormalFgHex;
    private TextBlock? _btnCautionHex;
    private TextBlock? _btnCautionFgHex;
    private TextBlock? _btnDangerHex;
    private TextBlock? _btnDangerFgHex;
    private TextBlock? _inputBgHex;
    private TextBlock? _inputFgHex;
    private TextBlock? _inputBorderHex;
    private TextBlock? _inputSelectionBgHex;
    private TextBlock? _inputSelectionFgHex;
    private TextBlock? _mutedFgHex;
    private TextBlock? _menuContrastLabel;
    private TextBlock? _mutedContrastLabel;
    private TextBlock? _buttonNormalContrastLabel;
    private TextBlock? _buttonCautionContrastLabel;
    private TextBlock? _buttonDangerContrastLabel;
    private TextBlock? _themeAaFlagLabel;
    private TextBlock? _menuAaFlagLabel;
    private TextBlock? _buttonAaFlagLabel;
    private TextBlock? _inputAaFlagLabel;
    private TextBlock? _themeAaFailuresLabel;
    private TextBlock? _menuAaFailuresLabel;
    private TextBlock? _buttonAaFailuresLabel;
    private TextBlock? _inputAaFailuresLabel;
    private Border? _themeColorCardBorder;
    private Border? _menuColorCardBorder;
    private Border? _buttonColorCardBorder;
    private Border? _inputColorCardBorder;
    private ListBox? _activeRadiosListBox;
    private RigRadioEditorWindow? _rigRadioEditorWindow;
    private bool _syncingActiveRadiosSelection;
    private DispatcherTimer? _contrastWarnTimer;
    private TextBlock? _hoverFontColorHex; // Add missing field declaration

    public ConfigurationWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(ConfigurationWindow));
        App.Toasts.RegisterWindow(this);
        _viewModel = new ConfigurationViewModel();
        DataContext = _viewModel;

        _contrastWarnTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        _contrastWarnTimer.Tick += (_, _) =>
        {
            _contrastWarnTimer.Stop();
            WarnOnContrastFailures();
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _bgPicker = this.FindControl<ColorPicker>("BgColorPicker");
        _fgPicker = this.FindControl<ColorPicker>("FgColorPicker");
        _menuBgPicker = this.FindControl<ColorPicker>("MenuBgColorPicker");
        _menuFgPicker = this.FindControl<ColorPicker>("MenuFgColorPicker");
        _btnNormalPicker = this.FindControl<ColorPicker>("BtnNormalColorPicker");
        _btnNormalFgPicker = this.FindControl<ColorPicker>("BtnNormalFgColorPicker");
        _btnCautionPicker = this.FindControl<ColorPicker>("BtnCautionColorPicker");
        _btnCautionFgPicker = this.FindControl<ColorPicker>("BtnCautionFgColorPicker");
        _btnDangerPicker = this.FindControl<ColorPicker>("BtnDangerColorPicker");
        _btnDangerFgPicker = this.FindControl<ColorPicker>("BtnDangerFgColorPicker");
        _inputBgPicker = this.FindControl<ColorPicker>("InputBgColorPicker");
        _inputFgPicker = this.FindControl<ColorPicker>("InputFgColorPicker");
        _inputBorderPicker = this.FindControl<ColorPicker>("InputBorderColorPicker");
        _inputSelectionBgPicker = this.FindControl<ColorPicker>("InputSelectionBgColorPicker");
        _inputSelectionFgPicker = this.FindControl<ColorPicker>("InputSelectionFgColorPicker");
        _mutedFgPicker = this.FindControl<ColorPicker>("MutedFgColorPicker");
        _hoverFontColorPicker = this.FindControl<ColorPicker>("HoverFontColorPicker");
        _bgHex    = this.FindControl<TextBlock>("BgColorHex");
        _fgHex    = this.FindControl<TextBlock>("FgColorHex");
        _menuBgHex = this.FindControl<TextBlock>("MenuBgColorHex");
        _menuFgHex = this.FindControl<TextBlock>("MenuFgColorHex");
        _btnNormalHex = this.FindControl<TextBlock>("BtnNormalColorHex");
        _btnNormalFgHex = this.FindControl<TextBlock>("BtnNormalFgColorHex");
        _btnCautionHex = this.FindControl<TextBlock>("BtnCautionColorHex");
        _btnCautionFgHex = this.FindControl<TextBlock>("BtnCautionFgColorHex");
        _btnDangerHex = this.FindControl<TextBlock>("BtnDangerColorHex");
        _btnDangerFgHex = this.FindControl<TextBlock>("BtnDangerFgColorHex");
        _inputBgHex = this.FindControl<TextBlock>("InputBgColorHex");
        _inputFgHex = this.FindControl<TextBlock>("InputFgColorHex");
        _inputBorderHex = this.FindControl<TextBlock>("InputBorderColorHex");
        _inputSelectionBgHex = this.FindControl<TextBlock>("InputSelectionBgColorHex");
        _inputSelectionFgHex = this.FindControl<TextBlock>("InputSelectionFgColorHex");
        _mutedFgHex = this.FindControl<TextBlock>("MutedFgColorHex");
        _hoverFontColorHex = this.FindControl<TextBlock>("HoverFontColorHex");
        _menuContrastLabel = this.FindControl<TextBlock>("MenuContrastLabel");
        _mutedContrastLabel = this.FindControl<TextBlock>("MutedContrastLabel");
        _buttonNormalContrastLabel = this.FindControl<TextBlock>("ButtonNormalContrastLabel");
        _buttonCautionContrastLabel = this.FindControl<TextBlock>("ButtonCautionContrastLabel");
        _buttonDangerContrastLabel = this.FindControl<TextBlock>("ButtonDangerContrastLabel");
        _themeAaFlagLabel = this.FindControl<TextBlock>("ThemeAaFlagLabel");
        _menuAaFlagLabel = this.FindControl<TextBlock>("MenuAaFlagLabel");
        _buttonAaFlagLabel = this.FindControl<TextBlock>("ButtonAaFlagLabel");
        _inputAaFlagLabel = this.FindControl<TextBlock>("InputAaFlagLabel");
        _themeAaFailuresLabel = this.FindControl<TextBlock>("ThemeAaFailuresLabel");
        _menuAaFailuresLabel = this.FindControl<TextBlock>("MenuAaFailuresLabel");
        _buttonAaFailuresLabel = this.FindControl<TextBlock>("ButtonAaFailuresLabel");
        _inputAaFailuresLabel = this.FindControl<TextBlock>("InputAaFailuresLabel");
        _themeColorCardBorder = this.FindControl<Border>("ThemeColorCardBorder");
        _menuColorCardBorder = this.FindControl<Border>("MenuColorCardBorder");
        _buttonColorCardBorder = this.FindControl<Border>("ButtonColorCardBorder");
        _inputColorCardBorder = this.FindControl<Border>("InputColorCardBorder");
        _activeRadiosListBox = this.FindControl<ListBox>("ActiveRadiosListBox");

        // Push initial colors into pickers now that controls are fully rendered
        SyncPickersFromViewModel();

        // ViewModel → pickers (profile switch)
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        SyncActiveRadioSelectionsFromViewModel();

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

        if (_btnNormalFgPicker != null)
            _btnNormalFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonNormalForegroundColor = ev.NewColor;
                _viewModel.ButtonForegroundColor = ev.NewColor;
                UpdateHex(_btnNormalFgHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnCautionPicker != null)
            _btnCautionPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonCautionColor = ev.NewColor;
                UpdateHex(_btnCautionHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnCautionFgPicker != null)
            _btnCautionFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonCautionForegroundColor = ev.NewColor;
                UpdateHex(_btnCautionFgHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnDangerPicker != null)
            _btnDangerPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonDangerColor = ev.NewColor;
                UpdateHex(_btnDangerHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_btnDangerFgPicker != null)
            _btnDangerFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.ButtonDangerForegroundColor = ev.NewColor;
                UpdateHex(_btnDangerFgHex, ev.NewColor);
                UpdateContrastLabels();
            };

        if (_inputBgPicker != null)
            _inputBgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.InputBackgroundColor = ev.NewColor;
                UpdateHex(_inputBgHex, ev.NewColor);
            };

        if (_inputFgPicker != null)
            _inputFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.InputForegroundColor = ev.NewColor;
                UpdateHex(_inputFgHex, ev.NewColor);
            };

        if (_inputBorderPicker != null)
            _inputBorderPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.InputBorderColor = ev.NewColor;
                UpdateHex(_inputBorderHex, ev.NewColor);
            };

        if (_inputSelectionBgPicker != null)
            _inputSelectionBgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.InputSelectionBackgroundColor = ev.NewColor;
                UpdateHex(_inputSelectionBgHex, ev.NewColor);
            };

        if (_inputSelectionFgPicker != null)
            _inputSelectionFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.InputSelectionForegroundColor = ev.NewColor;
                UpdateHex(_inputSelectionFgHex, ev.NewColor);
            };

        if (_mutedFgPicker != null)
            _mutedFgPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.MutedForegroundColor = ev.NewColor;
                UpdateHex(_mutedFgHex, ev.NewColor);
            };

        if (_hoverFontColorPicker != null)
            _hoverFontColorPicker.ColorChanged += (_, ev) =>
            {
                _viewModel.HoverFontColor = ev.NewColor;
                UpdateHex(_hoverFontColorHex, ev.NewColor);
            };

        UpdateContrastLabels();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var needsContrastRefresh = false;
        if (e.PropertyName is nameof(ConfigurationViewModel.BackgroundColor))
        {
            SyncPickerColor(_bgPicker, _bgHex, _viewModel.BackgroundColor);
            needsContrastRefresh = true;
        }
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
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonNormalForegroundColor))
        {
            SyncPickerColor(_btnNormalFgPicker, _btnNormalFgHex, _viewModel.ButtonNormalForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonCautionColor))
        {
            SyncPickerColor(_btnCautionPicker, _btnCautionHex, _viewModel.ButtonCautionColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonCautionForegroundColor))
        {
            SyncPickerColor(_btnCautionFgPicker, _btnCautionFgHex, _viewModel.ButtonCautionForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonDangerColor))
        {
            SyncPickerColor(_btnDangerPicker, _btnDangerHex, _viewModel.ButtonDangerColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.ButtonDangerForegroundColor))
        {
            SyncPickerColor(_btnDangerFgPicker, _btnDangerFgHex, _viewModel.ButtonDangerForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.InputBackgroundColor))
        {
            SyncPickerColor(_inputBgPicker, _inputBgHex, _viewModel.InputBackgroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.InputForegroundColor))
        {
            SyncPickerColor(_inputFgPicker, _inputFgHex, _viewModel.InputForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.InputBorderColor))
            SyncPickerColor(_inputBorderPicker, _inputBorderHex, _viewModel.InputBorderColor);
        if (e.PropertyName is nameof(ConfigurationViewModel.InputSelectionBackgroundColor))
        {
            SyncPickerColor(_inputSelectionBgPicker, _inputSelectionBgHex, _viewModel.InputSelectionBackgroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.InputSelectionForegroundColor))
        {
            SyncPickerColor(_inputSelectionFgPicker, _inputSelectionFgHex, _viewModel.InputSelectionForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.MutedForegroundColor))
        {
            SyncPickerColor(_mutedFgPicker, _mutedFgHex, _viewModel.MutedForegroundColor);
            needsContrastRefresh = true;
        }
        if (e.PropertyName is nameof(ConfigurationViewModel.HoverFontColor))
        {
            SyncPickerColor(_hoverFontColorPicker, _hoverFontColorHex, _viewModel.HoverFontColor);
            needsContrastRefresh = true;
        }

        if (needsContrastRefresh)
            UpdateContrastLabels();

        if (e.PropertyName is nameof(ConfigurationViewModel.SelectedProfile) or nameof(ConfigurationViewModel.AvailableRigRadioOptions))
            SyncActiveRadioSelectionsFromViewModel();
    }

    private void SyncPickersFromViewModel()
    {
        SyncPickerColor(_bgPicker, _bgHex, _viewModel.BackgroundColor);
        SyncPickerColor(_fgPicker, _fgHex, _viewModel.ForegroundColor);
        SyncPickerColor(_menuBgPicker, _menuBgHex, _viewModel.MenuBackgroundColor);
        SyncPickerColor(_menuFgPicker, _menuFgHex, _viewModel.MenuForegroundColor);
        SyncPickerColor(_btnNormalPicker, _btnNormalHex, _viewModel.ButtonNormalColor);
        SyncPickerColor(_btnNormalFgPicker, _btnNormalFgHex, _viewModel.ButtonNormalForegroundColor);
        SyncPickerColor(_btnCautionPicker, _btnCautionHex, _viewModel.ButtonCautionColor);
        SyncPickerColor(_btnCautionFgPicker, _btnCautionFgHex, _viewModel.ButtonCautionForegroundColor);
        SyncPickerColor(_btnDangerPicker, _btnDangerHex, _viewModel.ButtonDangerColor);
        SyncPickerColor(_btnDangerFgPicker, _btnDangerFgHex, _viewModel.ButtonDangerForegroundColor);
        SyncPickerColor(_inputBgPicker, _inputBgHex, _viewModel.InputBackgroundColor);
        SyncPickerColor(_inputFgPicker, _inputFgHex, _viewModel.InputForegroundColor);
        SyncPickerColor(_inputBorderPicker, _inputBorderHex, _viewModel.InputBorderColor);
        SyncPickerColor(_inputSelectionBgPicker, _inputSelectionBgHex, _viewModel.InputSelectionBackgroundColor);
        SyncPickerColor(_inputSelectionFgPicker, _inputSelectionFgHex, _viewModel.InputSelectionForegroundColor);
        SyncPickerColor(_mutedFgPicker, _mutedFgHex, _viewModel.MutedForegroundColor);
        SyncPickerColor(_hoverFontColorPicker, _hoverFontColorHex, _viewModel.HoverFontColor);
        UpdateContrastLabels();
    }

    private void UpdateContrastLabels()
    {
        SetContrastLabel(_menuContrastLabel, "Menu contrast", _viewModel.MenuForegroundColor, _viewModel.MenuBackgroundColor);
        SetContrastLabel(_mutedContrastLabel, "Muted labels", _viewModel.MutedForegroundColor, GetPanelBackgroundColor());
        SetContrastLabel(_buttonNormalContrastLabel, "Button normal", _viewModel.ButtonNormalForegroundColor, _viewModel.ButtonNormalColor);
        SetContrastLabel(_buttonCautionContrastLabel, "Button caution", _viewModel.ButtonCautionForegroundColor, _viewModel.ButtonCautionColor);
        SetContrastLabel(_buttonDangerContrastLabel, "Button danger", _viewModel.ButtonDangerForegroundColor, _viewModel.ButtonDangerColor);

        var themeFailures = new List<string>();
        var menuFailures = new List<string>();
        var buttonFailures = new List<string>();
        var inputFailures = new List<string>();

        var themePasses = TrackAaPair(themeFailures, "Foreground on background", _viewModel.ForegroundColor, _viewModel.BackgroundColor)
            && TrackAaPair(themeFailures, "Muted labels on panel", _viewModel.MutedForegroundColor, GetPanelBackgroundColor());
        var menuPasses = TrackAaPair(menuFailures, "Menu text", _viewModel.MenuForegroundColor, _viewModel.MenuBackgroundColor);
        var buttonPasses = TrackAaPair(buttonFailures, "Normal button", _viewModel.ButtonNormalForegroundColor, _viewModel.ButtonNormalColor)
            && TrackAaPair(buttonFailures, "Caution button", _viewModel.ButtonCautionForegroundColor, _viewModel.ButtonCautionColor)
            && TrackAaPair(buttonFailures, "Danger button", _viewModel.ButtonDangerForegroundColor, _viewModel.ButtonDangerColor);
        var inputPasses = TrackAaPair(inputFailures, "Input text", _viewModel.InputForegroundColor, _viewModel.InputBackgroundColor)
            && TrackAaPair(inputFailures, "Selection text", _viewModel.InputSelectionForegroundColor, _viewModel.InputSelectionBackgroundColor);

        SetGroupAaFlag(_themeAaFlagLabel, themePasses);
        SetGroupAaFlag(_menuAaFlagLabel, menuPasses);
        SetGroupAaFlag(_buttonAaFlagLabel, buttonPasses);
        SetGroupAaFlag(_inputAaFlagLabel, inputPasses);

        SetFailureSummaryLabel(_themeAaFailuresLabel, themeFailures);
        SetFailureSummaryLabel(_menuAaFailuresLabel, menuFailures);
        SetFailureSummaryLabel(_buttonAaFailuresLabel, buttonFailures);
        SetFailureSummaryLabel(_inputAaFailuresLabel, inputFailures);

        SetCardAaState(_themeColorCardBorder, themePasses);
        SetCardAaState(_menuColorCardBorder, menuPasses);
        SetCardAaState(_buttonColorCardBorder, buttonPasses);
        SetCardAaState(_inputColorCardBorder, inputPasses);

        UpdateAaFieldFlags();

        // Debounce the toast so it doesn't fire on every drag tick
        _contrastWarnTimer?.Stop();
        _contrastWarnTimer?.Start();
    }

    private void UpdateAaFieldFlags()
    {
        var failingHexLabels = new HashSet<TextBlock>();
        MarkFailingPair(failingHexLabels, _fgHex, _bgHex, _viewModel.ForegroundColor, _viewModel.BackgroundColor);
        MarkFailingPair(failingHexLabels, _mutedFgHex, _bgHex, _viewModel.MutedForegroundColor, GetPanelBackgroundColor());
        MarkFailingPair(failingHexLabels, _menuFgHex, _menuBgHex, _viewModel.MenuForegroundColor, _viewModel.MenuBackgroundColor);
        MarkFailingPair(failingHexLabels, _btnNormalFgHex, _btnNormalHex, _viewModel.ButtonNormalForegroundColor, _viewModel.ButtonNormalColor);
        MarkFailingPair(failingHexLabels, _btnCautionFgHex, _btnCautionHex, _viewModel.ButtonCautionForegroundColor, _viewModel.ButtonCautionColor);
        MarkFailingPair(failingHexLabels, _btnDangerFgHex, _btnDangerHex, _viewModel.ButtonDangerForegroundColor, _viewModel.ButtonDangerColor);
        MarkFailingPair(failingHexLabels, _inputFgHex, _inputBgHex, _viewModel.InputForegroundColor, _viewModel.InputBackgroundColor);
        MarkFailingPair(failingHexLabels, _inputSelectionFgHex, _inputSelectionBgHex, _viewModel.InputSelectionForegroundColor, _viewModel.InputSelectionBackgroundColor);

        SetHexAaFlag(_bgHex, IsFlagged(failingHexLabels, _bgHex));
        SetHexAaFlag(_fgHex, IsFlagged(failingHexLabels, _fgHex));
        SetHexAaFlag(_menuBgHex, IsFlagged(failingHexLabels, _menuBgHex));
        SetHexAaFlag(_menuFgHex, IsFlagged(failingHexLabels, _menuFgHex));
        SetHexAaFlag(_btnNormalHex, IsFlagged(failingHexLabels, _btnNormalHex));
        SetHexAaFlag(_btnNormalFgHex, IsFlagged(failingHexLabels, _btnNormalFgHex));
        SetHexAaFlag(_btnCautionHex, IsFlagged(failingHexLabels, _btnCautionHex));
        SetHexAaFlag(_btnCautionFgHex, IsFlagged(failingHexLabels, _btnCautionFgHex));
        SetHexAaFlag(_btnDangerHex, IsFlagged(failingHexLabels, _btnDangerHex));
        SetHexAaFlag(_btnDangerFgHex, IsFlagged(failingHexLabels, _btnDangerFgHex));
        SetHexAaFlag(_inputBgHex, IsFlagged(failingHexLabels, _inputBgHex));
        SetHexAaFlag(_inputFgHex, IsFlagged(failingHexLabels, _inputFgHex));
        SetHexAaFlag(_inputSelectionBgHex, IsFlagged(failingHexLabels, _inputSelectionBgHex));
        SetHexAaFlag(_inputSelectionFgHex, IsFlagged(failingHexLabels, _inputSelectionFgHex));
        SetHexAaFlag(_mutedFgHex, IsFlagged(failingHexLabels, _mutedFgHex));
        SetHexAaFlag(_inputBorderHex, false);
        SetHexAaFlag(_hoverFontColorHex, false);
    }

    private static bool TrackAaPair(List<string> failures, string label, Color foreground, Color background)
    {
        var passes = GetContrastRatio(foreground, background) >= 4.5;
        if (!passes)
            failures.Add(label);
        return passes;
    }

    private static bool IsFlagged(HashSet<TextBlock> failingHexLabels, TextBlock? label)
        => label is not null && failingHexLabels.Contains(label);

    private static void SetFailureSummaryLabel(TextBlock? label, IReadOnlyCollection<string> failures)
    {
        if (label is null)
            return;

        label.FontWeight = FontWeight.SemiBold;

        if (failures.Count == 0)
        {
            label.Text = "All monitored pairs meet AA";
            label.Foreground = new SolidColorBrush(Color.Parse("#4ADE80"));
            return;
        }

        label.Text = $"Needs adjustment: {string.Join(", ", failures)}";
        label.Foreground = new SolidColorBrush(Color.Parse("#FBBF24"));
    }

    private static void SetCardAaState(Border? cardBorder, bool isAa)
    {
        if (cardBorder is null)
            return;

        // Keep the card's themed background; only adjust border emphasis for AA state.
        cardBorder.ClearValue(Border.BackgroundProperty);
        cardBorder.BorderBrush = new SolidColorBrush(isAa ? Color.Parse("#22C55E") : Color.Parse("#F59E0B"));
        cardBorder.BorderThickness = isAa ? new Thickness(1) : new Thickness(2);
    }

    private static void MarkFailingPair(HashSet<TextBlock> failingHexLabels, TextBlock? fgLabel, TextBlock? bgLabel, Color foreground, Color background)
    {
        if (GetContrastRatio(foreground, background) >= 4.5)
            return;

        if (fgLabel != null)
            failingHexLabels.Add(fgLabel);
        if (bgLabel != null)
            failingHexLabels.Add(bgLabel);
    }

    private static void SetHexAaFlag(TextBlock? label, bool failsAa)
    {
        if (label is null)
            return;

        const string marker = "  !AA";
        var baseText = label.Text?.EndsWith(marker, StringComparison.Ordinal) == true
            ? label.Text[..^marker.Length]
            : label.Text ?? string.Empty;

        label.Text = failsAa ? $"{baseText}{marker}" : baseText;

        if (failsAa)
        {
            label.Foreground = new SolidColorBrush(Color.Parse("#FBBF24"));
            label.FontWeight = FontWeight.SemiBold;
            return;
        }

        label.ClearValue(TextBlock.ForegroundProperty);
        label.ClearValue(TextBlock.FontWeightProperty);
    }

    private void WarnOnContrastFailures()
    {
        var pairs = new (string Name, Color Fg, Color Bg)[]
        {
            ("Background / Foreground", _viewModel.ForegroundColor, _viewModel.BackgroundColor),
            ("Muted Labels (panel)", _viewModel.MutedForegroundColor, GetPanelBackgroundColor()),
            ("Menu", _viewModel.MenuForegroundColor, _viewModel.MenuBackgroundColor),
            ("Button Normal", _viewModel.ButtonNormalForegroundColor, _viewModel.ButtonNormalColor),
            ("Button Caution", _viewModel.ButtonCautionForegroundColor, _viewModel.ButtonCautionColor),
            ("Button Danger", _viewModel.ButtonDangerForegroundColor, _viewModel.ButtonDangerColor),
            ("Input", _viewModel.InputForegroundColor, _viewModel.InputBackgroundColor),
            ("Input Selection", _viewModel.InputSelectionForegroundColor, _viewModel.InputSelectionBackgroundColor),
        };

        var failures = pairs
            .Select(p => (p.Name, Ratio: GetContrastRatio(p.Fg, p.Bg)))
            .Where(x => x.Ratio < 4.5)
            .ToList();

        if (failures.Count == 0)
            return;

        var lines = failures.Select(f =>
        {
            var grade = f.Ratio >= 3.0 ? "large text only" : "fails";
            return $"{f.Name}: {f.Ratio:0.00}:1 ({grade})";
        });

        var message =
            "Some UI color pairs are hard to read. " +
            "Target contrast is 4.5:1 or higher for normal text.\n" +
            "Adjust foreground/background colors in the Colors tab, then re-check.\n\n" +
            string.Join("\n", lines);

        App.Toasts.ShowWarning(
            "Accessibility contrast warning",
            message);
    }

    private static void SetContrastLabel(TextBlock? label, string title, Color foreground, Color background)
    {
        if (label is null)
            return;

        var ratio = GetContrastRatio(foreground, background);
        var grade = ratio >= 7.0 ? "AAA" : ratio >= 4.5 ? "AA" : ratio >= 3.0 ? "Large text only" : "Fail";
        label.Text = $"{title}: {ratio:0.00}:1 ({grade})";
    }

    private static void SetGroupAaFlag(TextBlock? label, bool isAa)
    {
        if (label is null)
            return;

        label.Text = isAa ? "AA OK" : "Below AA";
        label.Foreground = new SolidColorBrush(isAa ? Color.Parse("#4ADE80") : Color.Parse("#FBBF24"));
        label.FontWeight = FontWeight.SemiBold;
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

    private Color GetPanelBackgroundColor() => AdjustBrightness(_viewModel.BackgroundColor, 0.08);

    private static Color AdjustBrightness(Color color, double delta)
    {
        byte Clamp(double value) => (byte)Math.Max(0, Math.Min(255, value));
        return Color.FromArgb(color.A, Clamp(color.R + 255 * delta), Clamp(color.G + 255 * delta), Clamp(color.B + 255 * delta));
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
    public async void OnAddRadioClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.AddRigRadio();
        if (_rigRadioEditorWindow is { IsVisible: true })
        {
            _rigRadioEditorWindow.Activate();
            return;
        }

        if (App.ActivateOpenWindow<RigRadioEditorWindow>())
            return;

        _rigRadioEditorWindow = new RigRadioEditorWindow(_viewModel);
        _rigRadioEditorWindow.Closed += (_, _) => _rigRadioEditorWindow = null;
        await _rigRadioEditorWindow.ShowDialog(this);
    }
    public void OnRemoveRadioClicked(object? sender, RoutedEventArgs e) => _viewModel.RemoveSelectedRigRadio();
    public async void OnEditSelectedRadioClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.RevertSelectedRigRadioEdits();
        if (_rigRadioEditorWindow is { IsVisible: true })
        {
            _rigRadioEditorWindow.Activate();
            return;
        }

        if (App.ActivateOpenWindow<RigRadioEditorWindow>())
            return;

        _rigRadioEditorWindow = new RigRadioEditorWindow(_viewModel);
        _rigRadioEditorWindow.Closed += (_, _) => _rigRadioEditorWindow = null;
        await _rigRadioEditorWindow.ShowDialog(this);
    }
    public void OnRefreshSerialPortsClicked(object? sender, RoutedEventArgs e) => _viewModel.RefreshSerialPorts();
    public void OnResetColorsClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.ResetColorsToDefaults();
        UpdateContrastLabels();
    }
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

    public void OnCatalogSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        var selected = grid.SelectedItems?.Cast<RigCatalogEntry>() ?? [];
        _viewModel.RigCatalog.SetSelectedEntries(selected);
    }

    public void OnActiveRigRadiosSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingActiveRadiosSelection || sender is not ListBox listBox)
            return;

        var selectedNames = listBox.SelectedItems?
            .OfType<RigRadioOption>()
            .Select(x => x.RadioName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList() ?? [];
        _viewModel.SetActiveRigRadioNames(selectedNames);
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

    public void OnLoadRiglistClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.RiglistFilePath))
        {
            _viewModel.RigCatalog.SetStatusMessage("Rig list path is empty.");
            return;
        }

        _viewModel.RigCatalog.LoadFromFile(_viewModel.RiglistFilePath);
    }

    public async void OnBrowseAdifDirectoryClicked(object? sender, RoutedEventArgs e)
    {
        if (!StorageProvider.CanPickFolder)
            return;

        var suggestedStartLocation = await TryGetFolderFromPathAsync(_viewModel.AdifDirectory);
        var folders = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Select ADIF import directory",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation
        });

        if (folders.Count > 0)
            _viewModel.AdifDirectory = folders[0].Path.LocalPath;
    }

    public async void OnBrowseDatabaseDirectoryClicked(object? sender, RoutedEventArgs e)
    {
        if (!StorageProvider.CanPickFolder)
            return;

        var suggestedStartLocation = await TryGetFolderFromPathAsync(_viewModel.DatabaseFolderPath);
        var folders = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Select SQLite database folder",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation
        });

        if (folders.Count > 0)
            _viewModel.DatabaseFolderPath = folders[0].Path.LocalPath;
    }

    private async Task<Avalonia.Platform.Storage.IStorageFolder?> TryGetFolderFromPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var candidate = path;
        if (File.Exists(candidate))
        {
            candidate = Path.GetDirectoryName(candidate);
            if (string.IsNullOrWhiteSpace(candidate))
                return null;
        }

        if (!Directory.Exists(candidate))
            return null;

        var fullPath = Path.GetFullPath(candidate);
        if (!Path.EndsInDirectorySeparator(fullPath))
            fullPath += Path.DirectorySeparatorChar;

        return await StorageProvider.TryGetFolderFromPathAsync(new Uri(fullPath));
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void SyncActiveRadioSelectionsFromViewModel()
    {
        if (_activeRadiosListBox?.ItemsSource is not IEnumerable<RigRadioOption> options)
            return;
        var selectedItems = _activeRadiosListBox.SelectedItems;
        if (selectedItems is null)
            return;

        _syncingActiveRadiosSelection = true;
        try
        {
            var selectedNameSet = _viewModel.GetActiveRigRadioNames()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            selectedItems.Clear();
            foreach (var option in options.Where(x => selectedNameSet.Contains(x.RadioName)))
                selectedItems.Add(option);
        }
        finally
        {
            _syncingActiveRadiosSelection = false;
        }
    }
}
