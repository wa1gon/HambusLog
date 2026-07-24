namespace HamBusLog.Views;

using Avalonia.VisualTree;
using Avalonia.Threading;
using HamBusLog.Data.Repositories.Sqlite;
using HamBusLog.Wa1gonLib.Models;

public partial class MainWindow
{
    private MenuNode? _previousSelection;
    private GridWindow? _gridWindow;
    private LogInputWindow? _logInputWindow;
    private SqliteQsoRepository? _qsoRepository;
    private ConfigurationWindow? _configurationWindow;
    private DxSpotsWindow? _dxSpotsWindow;
    private WsjtDebugWindow? _wsjtDebugWindow;
    private DigitalVoiceKeyerWindow? _digitalVoiceKeyerWindow;
    private CabrilloExportWindow? _cabrilloExportWindow;
    private ArqpProgressWindow? _arqpProgressWindow;
    private ArrlFdProgressWindow? _arrlFdProgressWindow;
    private LotwUploadWindow? _lotwUploadWindow;
    private CancellationTokenSource? _dashboardPlaybackCts;
    private int? _dashboardPlaybackSlot;
    private bool _isImportingAdif;
    private bool _isAppExitRequested;
    private bool _skipNextMenuSelectionChanged;
    private bool _isHandlingMenuClick;
    private bool _startupFocusPulseApplied;
    private bool _hasReceivedWindowPointerEvent;
    private CancellationTokenSource? _activationGuardCts;
    private DateTime _openedUtc;
    private DateTime _lastQuickActionDispatchUtc;
    private string? _lastQuickActionDispatchName;

    public MainWindow()
    {
        InitializeComponent();
        ApplyStayOnTopSetting();
        // MainWindow placement tracking is disabled to avoid close-button hangs on Linux.
        App.Toasts.RegisterWindow(this);
        
        // Capture pointer events at multiple stages to ensure we see all events
        AddHandler(InputElement.PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        
        // Also listen for PointerMoved to detect when WM releases input grab
        AddHandler(InputElement.PointerMovedEvent, OnWindowPointerMoved, RoutingStrategies.Tunnel);
        
        Opened += (_, _) =>
        {
            _openedUtc = DateTime.UtcNow;
            _ = EnsureStartupFocusAsync();
        };

        Deactivated += (_, _) =>
        {
            // Some Linux WMs briefly deactivate the window during handoff; keep activation sticky
            // only when no other in-app window is active.
            var openedAgo = DateTime.UtcNow - _openedUtc;
            if (openedAgo <= TimeSpan.FromSeconds(8) && !IsAnotherAppWindowActive())
                StartActivationGuard("early-deactivated", 3000);
        };
    }

    private void OnWindowPointerMoved(object? sender, PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Pointer.Type == PointerType.Mouse && !_hasReceivedWindowPointerEvent)
        {
            // This is the first pointer movement after the window opened
            // By this point, the WM should have released its focus grab
            _hasReceivedWindowPointerEvent = true;
            // First post-open mouse movement observed.
        }

        // On some Linux WMs, first click is consumed to activate window.
        // Proactively activate on mouse movement so the next click reaches buttons.
        if (!IsActive)
        {
            Activate();
            Focus();
        }
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Pointer.Type != PointerType.Mouse)
            return;

        _hasReceivedWindowPointerEvent = true;
        // First actionable mouse press can be swallowed by WM focus handoff.
        // Route quick-action clicks directly from window-level pointer events.
        if (!e.Handled && TryDispatchQuickActionFromSource(e.Source))
            e.Handled = true;
    }

    private bool TryDispatchQuickActionFromSource(object? source)
    {
        if (source is not Visual visual)
            return false;

        var button = visual.FindAncestorOfType<Button>();
        if (button is null)
            return false;

        switch (button.Name)
        {
            case "ProgressStatusQuickActionButton":
                OnOpenProgressStatusClicked(button, new RoutedEventArgs());
                return true;
            case "OpenGridQuickActionButton":
                OnOpenGridClicked(button, new RoutedEventArgs());
                return true;
            case "NewQsoQuickActionButton":
                OnOpenNewContactClicked(button, new RoutedEventArgs());
                return true;
            case "DxClusterQuickActionButton":
                OnOpenDxClusterClicked(button, new RoutedEventArgs());
                return true;
            case "VoiceKeyerQuickActionButton":
                OnOpenDigitalVoiceKeyerClicked(button, new RoutedEventArgs());
                return true;
            case "ExitProgramQuickActionButton":
                OnExitProgramClicked(button, new RoutedEventArgs());
                return true;
            default:
                return false;
        }
    }

    private bool ShouldHandleQuickActionInvocation(object? sender, string expectedButtonName)
    {
        if (sender is not Button button)
            return true;

        if (!string.Equals(button.Name, expectedButtonName, StringComparison.Ordinal))
            return true;

        return TryBeginQuickActionDispatch(button.Name);
    }

    private bool TryBeginQuickActionDispatch(string? buttonName, int dedupeMs = 700)
    {
        if (string.IsNullOrWhiteSpace(buttonName))
            return false;

        var now = DateTime.UtcNow;
        if (string.Equals(_lastQuickActionDispatchName, buttonName, StringComparison.Ordinal)
            && (now - _lastQuickActionDispatchUtc).TotalMilliseconds < dedupeMs)
        {
            return false;
        }

        _lastQuickActionDispatchName = buttonName;
        _lastQuickActionDispatchUtc = now;
        return true;
    }

    private void StartActivationGuard(string reason, int durationMs = 2200)
    {
        _activationGuardCts?.Cancel();
        _activationGuardCts?.Dispose();

        var cts = new CancellationTokenSource();
        _activationGuardCts = cts;

        _ = Task.Run(async () =>
        {
            var deadlineUtc = DateTime.UtcNow.AddMilliseconds(durationMs);
            while (DateTime.UtcNow < deadlineUtc && !cts.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!IsVisible)
                        return;

                    if (!IsActive)
                    {
                        Activate();
                        Focus();
                    }
                }, DispatcherPriority.Input);

                try
                {
                    await Task.Delay(120, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        });
    }

    private bool IsAnotherAppWindowActive()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return false;

        return desktop.Windows.Any(w => !ReferenceEquals(w, this) && w.IsVisible && w.IsActive);
    }

    private async Task EnsureStartupFocusAsync()
    {
        if (_startupFocusPulseApplied)
            return;

        _startupFocusPulseApplied = true;

        try
        {
            // Quick initial focus
            Activate();
            Focus();
            await Task.Delay(50);

            // Check if we have focus
            if (!IsActive)
            {
                Topmost = true;
                Activate();
                Focus();
                await Task.Delay(50);
                Topmost = false;
            }

            // Force rendering by invalidating visual
            InvalidateVisual();
            InvalidateMeasure();
            InvalidateArrange();
            
            // Process dispatcher queue to ensure rendering completes
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.MaxValue);
            
            await Task.Delay(150);
        }
        finally
        {
            StartActivationGuard("startup");
        }
    }

    private void ApplyStayOnTopSetting()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        Topmost = profile.StayOnTopMainWindow;

        var checkBox = this.FindControl<CheckBox>("StayOnTopCheckBox");
        if (checkBox is not null)
            checkBox.IsChecked = Topmost;
    }

    private void SaveStayOnTopSetting(bool isEnabled)
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        profile.StayOnTopMainWindow = isEnabled;
        AppConfigurationStore.Save(config);
    }

    private void UpdateStayOnTop(bool isEnabled)
    {
        Topmost = isEnabled;
        SaveStayOnTopSetting(isEnabled);
    }

    private void OnStayOnTopChecked(object? sender, RoutedEventArgs e)
    {
        UpdateStayOnTop(true);
    }

    private void OnStayOnTopUnchecked(object? sender, RoutedEventArgs e)
    {
        UpdateStayOnTop(false);
    }

    public void RecoverFocusAfterSplashClose()
    {
        if (!IsVisible)
            return;

        Activate();
        Focus();
        StartActivationGuard("splash-closed", 3200);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        StopDashboardPlayback();
        _activationGuardCts?.Cancel();
        _activationGuardCts?.Dispose();
        _activationGuardCts = null;

        if (_isAppExitRequested)
        {
            base.OnClosing(e);
            return;
        }

        _isAppExitRequested = true;

        // Close auxiliary windows first; then allow MainWindow to close normally.
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows.ToList())
            {
                if (ReferenceEquals(window, this))
                    continue;

                try { window.Close(); } catch { }
            }
        }

        // Safety fallback: if the desktop lifetime fails to shut down, terminate process.
        _ = Task.Run(async () =>
        {
            await Task.Delay(1200);
            Environment.Exit(0);
        });

        base.OnClosing(e);
    }

    public async void OnMenuTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_skipNextMenuSelectionChanged)
        {
            _skipNextMenuSelectionChanged = false;
            return;
        }

        // Skip if we're already handling a menu click
        if (_isHandlingMenuClick)
        {
            return;
        }

        if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuNode node)
            await HandleMenuNodeClickAsync(node, sender);
    }

    public async void OnMenuTreeViewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual visual)
            return;

        var item = visual.FindAncestorOfType<TreeViewItem>();
        if (item?.DataContext is not MenuNode node)
            return;

        _skipNextMenuSelectionChanged = true;

        if (node.HasChildren)
        {
            item.IsExpanded = !item.IsExpanded;
            node.IsExpanded = item.IsExpanded;
            e.Handled = true;
            ResetTreeSelection(sender);
            return;
        }

        // Prevent duplicate handling
        if (_isHandlingMenuClick)
        {
            e.Handled = true;
            return;
        }

        _isHandlingMenuClick = true;
        try
        {
            await HandleMenuNodeClickAsync(node, sender);
        }
        finally
        {
            // Clear the flag after a brief delay to allow event processing
            _ = Task.Delay(100).ContinueWith(_ => _isHandlingMenuClick = false);
        }

        e.Handled = true;
    }

    private async Task HandleMenuNodeClickAsync(MenuNode node, object? sender)
    {
        if (node.Title == "Grid" || node.Title == "Open/Reopen Grid")
            ToggleGridWindow();
        else if (node.Title == "Add New Contact")
            OpenNewContactWindow();
        else if (node.Title == "Configuration")
            OpenConfigurationWindow();
        else if (node.Title == "Import ADIF")
            await ImportAdifAsync();
        else if (node.Title == "Import JADE")
            await ImportJadeAsync();
        else if (node.Title == "Export ADIF")
            await ExportAdifAsync();
        else if (node.Title == "Export JADE")
            await ExportJadeAsync();
        else if (node.Title == "Export JADE Schema")
            await ExportJadeSchemaAsync();
        else if (node.Title == "Export JADE Example")
            await ExportJadeExampleAsync();
        else if (node.Title == "Export Cabrillo")
            OpenCabrilloExportWindow();
        else if (node.Title == "DX Spots" || node.Title == "DX Cluster")
            ToggleDxSpotsWindow();
        else if (node.Title == "WSJT Debug")
            ToggleWsjtDebugWindow();
        else if (node.Title == "Digital Voice Keyer")
            ToggleDigitalVoiceKeyerWindow();
        else if (node.Title == "ARQP Report")
            OpenCabrilloExportWindow();
        else if (node.Title == "ARRL FD Report")
            OpenCabrilloExportWindow();
        else if (node.Title == "Callbook")
            ShowNotImplemented("Callbook");
        else if (node.Title == "Awards" && !node.HasChildren)
            ShowNotImplemented("Awards");
        else if (node.Title == "Logbook of the World")
            OpenLotwUploadWindow();
        else if (node.Title == "eLogs" && !node.HasChildren)
            ShowNotImplemented("eLogs");
        else if (node.Title == "RecCall")
            ShowNotImplemented("RecCall");
        else if (node.Title == "Net View")
            ShowNotImplemented("Net View");
        else if (node.Title == "Watch List")
            ShowNotImplemented("Watch List");
        else if (node.Title == "Remove Dups")
            ShowNotImplemented("Remove Dups");
        else if (node.Title == "About")
            await ShowSimpleModalAsync(
                "About HamBusLog",
                "HamBusLog\n\nAmateur radio logging with rig control, contest workflows, ADIF/JADE import-export, and DX tools.");
        else if (node.Title == "Credits")
            await ShowSimpleModalAsync(
                "Credits",
                "Credits\n\nChris K0SWE - dxcc-json\n\nBuilt by the HamBusLog contributors.");
        else
            _previousSelection = node;

        ResetTreeSelection(sender);
    }

    public void OnOpenGridClicked(object? sender, RoutedEventArgs e)
    {
        if (!ShouldHandleQuickActionInvocation(sender, "OpenGridQuickActionButton"))
            return;

        ToggleGridWindow();
    }

    public void OnOpenNewContactClicked(object? sender, RoutedEventArgs e)
    {
        if (!ShouldHandleQuickActionInvocation(sender, "NewQsoQuickActionButton"))
            return;

        OpenNewContactWindow();
    }


    public void OnOpenProgressStatusClicked(object? sender, RoutedEventArgs e)
    {
        if (!ShouldHandleQuickActionInvocation(sender, "ProgressStatusQuickActionButton"))
            return;

        var contest = App.LogTypeSelectionService.GetSelectedContestDefinition();
        var key  = contest.Key.Trim();
        var adif = contest.AdifContestId.Trim();
        var name = contest.DisplayName.Trim();

        if (contest.UsesFieldDayExchange)
        {
            OpenArrlFdProgressWindow();
            return;
        }

        if (IsArqpContestKey(key, adif, name))
        {
            OpenArqpProgressWindow();
            return;
        }

        App.Toasts.ShowInfo("Progress status", "No contest progress window is available for the selected contest.");
    }


    private static bool IsArqpContestKey(string key, string adif, string name)
    {
        static bool IsArqp(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return false;
            var u = v.Trim().ToUpperInvariant();
            return u is "ARQP" or "AR-QSO-PARTY";
        }
        return IsArqp(key) || IsArqp(adif)
               || name.Contains("Arkansas QSO Party", StringComparison.OrdinalIgnoreCase)
               || name.Contains("ARQP", StringComparison.OrdinalIgnoreCase);
    }

    public void OnOpenConfigurationClicked(object? sender, RoutedEventArgs e)
    {
        OpenConfigurationWindow();
    }

    public async void OnImportAdifClicked(object? sender, RoutedEventArgs e)
    {
        await ImportAdifAsync();
    }

    public void OnOpenDxClusterClicked(object? sender, RoutedEventArgs e)
    {
        if (!ShouldHandleQuickActionInvocation(sender, "DxClusterQuickActionButton"))
            return;

        ToggleDxSpotsWindow();
    }

    public void OnOpenDigitalVoiceKeyerClicked(object? sender, RoutedEventArgs e)
    {
        if (!ShouldHandleQuickActionInvocation(sender, "VoiceKeyerQuickActionButton"))
            return;

        ToggleDigitalVoiceKeyerWindow();
    }

    public async void OnVoiceKeyerSlotClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not DigitalVoiceKeyerDashboardSlotViewModel slot)
            return;

        if (_dashboardPlaybackCts is not null)
        {
            if (_dashboardPlaybackSlot == slot.SlotNumber)
            {
                StopDashboardPlayback();
                return;
            }

            StopDashboardPlayback();
        }

        var contest = App.LogTypeSelectionService.GetSelectedContestDefinition();
        if (!slot.HasRecording)
        {
            ToggleDigitalVoiceKeyerWindow();
            return;
        }

        var cts = new CancellationTokenSource();
        _dashboardPlaybackCts = cts;
        _dashboardPlaybackSlot = slot.SlotNumber;

        try
        {
            await App.DigitalVoiceKeyerService.PlaySlotAsync(contest.Key, slot.SlotNumber, cts.Token);
            if (cts.IsCancellationRequested)
                return;
        }
        finally
        {
            if (ReferenceEquals(_dashboardPlaybackCts, cts))
            {
                _dashboardPlaybackCts = null;
                _dashboardPlaybackSlot = null;
            }

            cts.Dispose();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Hotkeys for quick-action buttons
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            switch (e.Key)
            {
                case Key.N:
                    Log.Information("Alt+N pressed: Opening New QSO");
                    OpenNewContactWindow();
                    e.Handled = true;
                    return;
                case Key.G:
                    Log.Information("Alt+G pressed: Opening Grid");
                    ToggleGridWindow();
                    e.Handled = true;
                    return;
                case Key.C:
                    Log.Information("Alt+C pressed: Opening DX Cluster");
                    ToggleDxSpotsWindow();
                    e.Handled = true;
                    return;
                case Key.V:
                    Log.Information("Alt+V pressed: Opening Voice Keyer");
                    ToggleDigitalVoiceKeyerWindow();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    if (_dashboardPlaybackCts is not null)
                    {
                        StopDashboardPlayback();
                        e.Handled = true;
                        return;
                    }
                    break;
            }
        }

        base.OnKeyDown(e);
    }

    public void OnExitProgramClicked(object? sender, RoutedEventArgs e)
    {
        if (!ShouldHandleQuickActionInvocation(sender, "ExitProgramQuickActionButton"))
            return;

        Close();
    }

    private void ToggleGridWindow()
    {
        if (_gridWindow is { IsVisible: true })
        {
            App.SaveWindowPlacement(_gridWindow, nameof(GridWindow));
            _gridWindow.Hide();
            return;
        }

        if (_gridWindow is null)
        {
            _gridWindow = new GridWindow();
            _gridWindow.Closed += (_, _) => _gridWindow = null;
        }

        ShowWithVisibleOwner(_gridWindow);
    }

    private void OpenConfigurationWindow()
    {
        if (_configurationWindow is { IsVisible: true })
        {
            _configurationWindow.Activate();
            return;
        }

        if (_configurationWindow is null)
        {
            _configurationWindow = App.FindOpenWindow<ConfigurationWindow>() ?? new ConfigurationWindow();
            _configurationWindow.Closed += (_, _) => _configurationWindow = null;
        }

        ShowWithVisibleOwner(_configurationWindow);
    }

    private void ToggleDxSpotsWindow()
    {
        if (_dxSpotsWindow is { IsVisible: true })
        {
            App.SaveWindowPlacement(_dxSpotsWindow, nameof(DxSpotsWindow));
            _dxSpotsWindow.Hide();
            return;
        }

        if (_dxSpotsWindow is null)
        {
            _dxSpotsWindow = new DxSpotsWindow();
            _dxSpotsWindow.Closed += (_, _) => _dxSpotsWindow = null;
        }

        ShowWithVisibleOwner(_dxSpotsWindow);
    }

    private void ToggleWsjtDebugWindow()
    {
        if (_wsjtDebugWindow is { IsVisible: true })
        {
            App.SaveWindowPlacement(_wsjtDebugWindow, nameof(WsjtDebugWindow));
            _wsjtDebugWindow.Hide();
            return;
        }

        if (_wsjtDebugWindow is null)
        {
            _wsjtDebugWindow = new WsjtDebugWindow();
            _wsjtDebugWindow.Closed += (_, _) => _wsjtDebugWindow = null;
        }

        ShowWithVisibleOwner(_wsjtDebugWindow);
    }

    private void ToggleDigitalVoiceKeyerWindow()
    {
        if (_digitalVoiceKeyerWindow is { IsVisible: true })
        {
            App.SaveWindowPlacement(_digitalVoiceKeyerWindow, nameof(DigitalVoiceKeyerWindow));
            _digitalVoiceKeyerWindow.Hide();
            return;
        }

        if (_digitalVoiceKeyerWindow is null)
        {
            _digitalVoiceKeyerWindow = new DigitalVoiceKeyerWindow();
            _digitalVoiceKeyerWindow.Opened += OnDigitalVoiceKeyerWindowOpened;
            _digitalVoiceKeyerWindow.Closed += (_, _) => _digitalVoiceKeyerWindow = null;
        }

        _digitalVoiceKeyerWindow.Show();
        PositionDigitalVoiceKeyerOnDashboardMonitor();
        _digitalVoiceKeyerWindow.Activate();
    }

    private void OnDigitalVoiceKeyerWindowOpened(object? sender, EventArgs e)
    {
        PositionDigitalVoiceKeyerOnDashboardMonitor();
    }

    private void PositionDigitalVoiceKeyerOnDashboardMonitor()
    {
        if (_digitalVoiceKeyerWindow is null)
            return;

        _digitalVoiceKeyerWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        _digitalVoiceKeyerWindow.Position = new PixelPoint(Position.X + 24, Position.Y + 24);
    }

    private void OpenCabrilloExportWindow()
    {
        if (_cabrilloExportWindow is { IsVisible: true })
        {
            _cabrilloExportWindow.Activate();
            return;
        }

        if (_cabrilloExportWindow is null)
        {
            _cabrilloExportWindow = new CabrilloExportWindow();
            _cabrilloExportWindow.Closed += (_, _) => _cabrilloExportWindow = null;
        }

        ShowWithVisibleOwner(_cabrilloExportWindow);
    }

    private void OpenArqpProgressWindow()
    {
        if (_arqpProgressWindow is { IsVisible: true })
        {
            _arqpProgressWindow.Activate();
            return;
        }

        if (_arqpProgressWindow is null)
        {
            _arqpProgressWindow = App.FindOpenWindow<ArqpProgressWindow>() ?? new ArqpProgressWindow();
            _arqpProgressWindow.Closed += (_, _) => _arqpProgressWindow = null;
        }

        ShowWithVisibleOwner(_arqpProgressWindow);
    }

    private void OpenArrlFdProgressWindow()
    {
        if (_arrlFdProgressWindow is { IsVisible: true })
        {
            _arrlFdProgressWindow.Activate();
            return;
        }

        if (_arrlFdProgressWindow is null)
        {
            _arrlFdProgressWindow = App.FindOpenWindow<ArrlFdProgressWindow>() ?? new ArrlFdProgressWindow();
            _arrlFdProgressWindow.Closed += (_, _) => _arrlFdProgressWindow = null;
        }

        ShowWithVisibleOwner(_arrlFdProgressWindow);
    }

    private void OpenLotwUploadWindow()
    {
        if (_lotwUploadWindow is { IsVisible: true })
        {
            _lotwUploadWindow.Activate();
            return;
        }

        if (_lotwUploadWindow is null)
        {
            _lotwUploadWindow = new LotwUploadWindow();
            _lotwUploadWindow.Closed += (_, _) => _lotwUploadWindow = null;
        }

        ShowWithVisibleOwner(_lotwUploadWindow);
    }

    private void OpenNewContactWindow()
    {
        Log.Information("Opening new contact (QSO input) window");
        
        if (_logInputWindow is { IsVisible: true })
        {
            _logInputWindow.Activate();
            return;
        }

        var existingWindow = App.FindOpenWindow<LogInputWindow>();
        if (existingWindow is not null)
        {
            _logInputWindow = existingWindow;
            ShowLogInputWindow(existingWindow);
            return;
        }

        _qsoRepository ??= new SqliteQsoRepository(App.DbContext);
        _logInputWindow = new LogInputWindow();
        _logInputWindow.Closed += (_, _) =>
        {
            _logInputWindow = null;
            // Return focus to dashboard so first click is actionable.
            if (IsVisible)
            {
                Activate();
                Focus();
                if (!IsAnotherAppWindowActive())
                    StartActivationGuard("log-input-closed", 3200);
            }
        };
        _logInputWindow.QsoLogged += async (_, qso) => await SaveQsoAsync(qso);
        ShowLogInputWindow(_logInputWindow);
    }

    private void ShowLogInputWindow(LogInputWindow window)
    {
        if (window.IsVisible)
        {
            window.Activate();
            return;
        }

        if (IsVisible)
            window.Show(this);
        else
            window.Show();

        // Ensure window is properly activated and brought to foreground
        Dispatcher.UIThread.Post(() =>
        {
            window.Activate();
            window.Focus();
        });
    }

    private async Task SaveQsoAsync(Qso qso)
    {
        try
        {
            _qsoRepository ??= new SqliteQsoRepository(App.DbContext);
            await _qsoRepository.AddAsync(qso);
            await _qsoRepository.SaveChangesAsync();
            App.RaiseQsoSaved(qso);
            App.Toasts.ShowSuccess("QSO saved", $"{qso.Call} logged on {qso.Band} {qso.Mode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QSO: {ex.Message}");
            App.Toasts.ShowError("Save failed", ex.Message);
        }
    }

    private void ShowWithVisibleOwner(Window window)
    {
        window.Show();
        window.Activate();
    }

    private void StopDashboardPlayback()
    {
        var cts = _dashboardPlaybackCts;
        _dashboardPlaybackCts = null;
        _dashboardPlaybackSlot = null;

        if (cts is null)
            return;

        try
        {
            cts.Cancel();
        }
        catch
        {
        }
        finally
        {
            cts.Dispose();
        }
    }

    private void ResetTreeSelection(object? sender)
    {
        if (_previousSelection != null && sender is TreeView tv)
            tv.SelectedItem = _previousSelection;
    }

    private static void ShowNotImplemented(string featureName)
    {
        App.Toasts.ShowInfo(featureName, $"{featureName} is not wired yet. The menu flow is connected and ready for implementation.");
    }

    private async Task ShowSimpleModalAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            Height = 230,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var closeButton = new Button
        {
            Content = "Close",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            MinWidth = 88,
            Padding = new Thickness(12, 7)
        };
        closeButton.Click += (_, _) => dialog.Close();

        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 13
                    },
                    closeButton
                }
            }
        };

        var owner = GetPreferredDialogOwner() ?? this;
        await dialog.ShowDialog(owner);
    }

    private async Task ImportAdifAsync()
    {
        if (_isImportingAdif)
            return;

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select ADIF file to import",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeFilter =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("ADIF files") { Patterns = ["*.adi", "*.adif"] },
                new Avalonia.Platform.Storage.FilePickerFileType("All files") { Patterns = ["*"] }
            ]
        });

        if (files.Count == 0)
            return;

        var path = files[0].Path.LocalPath;
        var progressWindow = new AdifImportProgressWindow();
        var owner = GetPreferredDialogOwner();
        var progress = new Progress<AdifImportProgress>(update => progressWindow.UpdateProgress(update));
        AdifImportResult? result = null;
        Exception? importException = null;

        try
        {
            _isImportingAdif = true;
            progressWindow.UpdateProgress(AdifImportProgress.Starting(path));
            if (owner is not null)
                progressWindow.Show(owner);
            else
                progressWindow.Show();

            result = await Task.Run(() => AdifImportService.ImportFromFileAsync(path, progress: progress));
            RememberAdifDirectory(config, path);
        }
        catch (Exception ex)
        {
            importException = ex;
        }
        finally
        {
            if (result is null)
                progressWindow.UpdateProgress(AdifImportProgress.Completed(path, 0, 0));

            progressWindow.Close();
            _isImportingAdif = false;
        }

        if (importException is not null)
        {
            App.Toasts.ShowError("ADIF import failed", importException.Message);
            return;
        }

        if (result is not null)
        {
            if (result.Value.ParsedCount == 0)
            {
                App.Toasts.ShowWarning("ADIF import complete", "No QSO records were found in the selected file.");
                return;
            }

            App.Toasts.ShowSuccess(
                "ADIF import complete",
                $"Imported {result.Value.ParsedCount} QSO record(s). Database changes: {result.Value.SavedChanges}."
                + (result.Value.DuplicateCount > 0 ? $" Skipped duplicates: {result.Value.DuplicateCount}." : string.Empty));
        }
    }

    private async Task ImportJadeAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select JADE file to import",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeFilter =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] },
                new Avalonia.Platform.Storage.FilePickerFileType("All files") { Patterns = ["*"] }
            ]
        });

        if (files.Count == 0)
            return;

        var path = files[0].Path.LocalPath;
        var jadeImportOptions = new AdifImportOptions(
            DatabaseProvider.Sqlite,
            profile.ConnectionString,
            profile.StationCallSign);

        var progressWindow = new OperationProgressWindow("Importing JADE", $"Reading {Path.GetFileName(path)}...");
        var owner = GetPreferredDialogOwner();
        if (owner is not null)
            progressWindow.Show(owner);
        else
            progressWindow.Show();

        try
        {
            var imported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(path, jadeImportOptions);
            if (imported == 0)
            {
                App.Toasts.ShowWarning("JADE import complete", "No QSO records were found in the selected file.");
                return;
            }

            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE import complete", $"Imported {imported} QSO record(s).");
        }
        catch (HamBusLog.Wa1gonLib.Exchange.JadeValidationException ex)
        {
            var topErrors = ex.Errors.Take(4).ToList();
            var moreCount = Math.Max(0, ex.Errors.Count - topErrors.Count);
            var details = string.Join("\n", topErrors);
            if (string.IsNullOrWhiteSpace(profile.StationCallSign)
                && ex.Errors.Any(x => x.Contains("STATION_CALLSIGN", StringComparison.OrdinalIgnoreCase)))
            {
                details += "\nTip: Set Station Call Sign in Configuration before importing JADE files that omit STATION_CALLSIGN.";
            }
            if (moreCount > 0)
                details += $"\n...and {moreCount} more issue(s).";

            App.Toasts.ShowError("JADE import validation failed", details);
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE import failed", ex.Message);
        }
        finally
        {
            progressWindow.Close();
        }
    }

    private async Task ExportAdifAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export ADIF",
            SuggestedFileName = $"hambuslog-{DateTime.UtcNow:yyyyMMdd-HHmmss}.adi",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("ADIF files") { Patterns = ["*.adi", "*.adif"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        var progressWindow = new OperationProgressWindow("Exporting ADIF", $"Writing {Path.GetFileName(path)}...");
        var owner = GetPreferredDialogOwner();
        if (owner is not null)
            progressWindow.Show(owner);
        else
            progressWindow.Show();

        try
        {
            var exported = await AdifExportService.ExportToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("ADIF export complete", $"Exported {exported} QSO record(s).");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("ADIF export failed", ex.Message);
        }
        finally
        {
            progressWindow.Close();
        }
    }

    private async Task ExportJadeAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export JADE",
            SuggestedFileName = $"hambuslog-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        var progressWindow = new OperationProgressWindow("Exporting JADE", $"Writing {Path.GetFileName(path)}...");
        var owner = GetPreferredDialogOwner();
        if (owner is not null)
            progressWindow.Show(owner);
        else
            progressWindow.Show();

        try
        {
            var exported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE export complete", $"Exported {exported} QSO record(s).");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE export failed", ex.Message);
        }
        finally
        {
            progressWindow.Close();
        }
    }

    private async Task ExportJadeSchemaAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export JADE Schema Only",
            SuggestedFileName = "jade-schema-template.json",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        var progressWindow = new OperationProgressWindow("Exporting JADE Schema", $"Writing {Path.GetFileName(path)}...");
        var owner = GetPreferredDialogOwner();
        if (owner is not null)
            progressWindow.Show(owner);
        else
            progressWindow.Show();

        try
        {
            await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportSchemaToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE schema export complete", "Exported JADE schema/template with empty records array.");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE schema export failed", ex.Message);
        }
        finally
        {
            progressWindow.Close();
        }
    }

    private async Task ExportJadeExampleAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export JADE Example Record",
            SuggestedFileName = "jade-example-record.json",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        var progressWindow = new OperationProgressWindow("Exporting JADE Example", $"Writing {Path.GetFileName(path)}...");
        var owner = GetPreferredDialogOwner();
        if (owner is not null)
            progressWindow.Show(owner);
        else
            progressWindow.Show();

        try
        {
            await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportExampleToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE example export complete", "Exported JADE template with one populated example record.");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE example export failed", ex.Message);
        }
        finally
        {
            progressWindow.Close();
        }
    }

    private Window? GetPreferredDialogOwner()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return IsVisible ? this : null;

        return desktop.Windows.FirstOrDefault(window => window.IsActive)
               ?? desktop.Windows.FirstOrDefault(window => window.IsVisible)
               ?? (IsVisible ? this : null);
    }

    private static void RememberAdifDirectory(AppConfiguration config, string importedFilePath)
    {
        var directory = Path.GetDirectoryName(importedFilePath);
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

    private async void OnApplyRadioFrequencyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.ApplyFrequencyToSelectedRadioAsync();
    }

    private async void OnApplyRadioModeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.ApplyModeToSelectedRadioAsync();
    }

    private async void OnApplyPresetModeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (sender is not Button { Tag: string mode } || string.IsNullOrWhiteSpace(mode))
            return;

        await vm.ApplyPresetModeToSelectedRadioAsync(mode);
    }

    private void OnRadioRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (sender is Border { DataContext: RadioConnectionStatusViewModel row })
            vm.SelectedRadioStatus = row;
    }

    private void OnQuickActionPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Button button)
        {
            // Workaround: On first startup, the WM may consume the first pointer event
            // before it reaches the window. If we haven't received a window-level pointer
            // event yet, signal that the system is now interactive.
            if (!_hasReceivedWindowPointerEvent)
            {
                _hasReceivedWindowPointerEvent = true;
            }
        }
    }

    private void OnQuickActionPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Button button)
            return;

        var point = e.GetCurrentPoint(this);
        if (point.Pointer.Type != PointerType.Mouse)
            return;

        // Use pointer release as the action trigger to avoid lost first Click events on some Linux WMs.
        switch (button.Name)
        {
            case "ProgressStatusQuickActionButton":
                OnOpenProgressStatusClicked(button, new RoutedEventArgs());
                break;
            case "OpenGridQuickActionButton":
                OnOpenGridClicked(button, new RoutedEventArgs());
                break;
            case "NewQsoQuickActionButton":
                OnOpenNewContactClicked(button, new RoutedEventArgs());
                break;
            case "DxClusterQuickActionButton":
                OnOpenDxClusterClicked(button, new RoutedEventArgs());
                break;
            case "VoiceKeyerQuickActionButton":
                OnOpenDigitalVoiceKeyerClicked(button, new RoutedEventArgs());
                break;
            case "ExitProgramQuickActionButton":
                OnExitProgramClicked(button, new RoutedEventArgs());
                break;
            default:
                return;
        }

        e.Handled = true;
    }
}
