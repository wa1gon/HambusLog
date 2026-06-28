using Avalonia.Threading;
using HamBusLog.Wa1gonLib.Models;
using HamBusLog.Data;
using HamBusLog.Services;

namespace HamBusLog.Views;

public partial class LogInputWindow
{
    private readonly LogInputViewModel _viewModel;
    private readonly DispatcherTimer _activeRigRefreshTimer = new() { Interval = TimeSpan.FromSeconds(5) };

    /// <summary>Raised when the user successfully logs a QSO.</summary>
    public event EventHandler<Qso>? QsoLogged;

    public LogInputWindow()
        : this(null, null, null)
    {
    }

    public LogInputWindow(string? initialCallsign)
        : this(initialCallsign, null, null)
    {
    }

    public LogInputWindow(string? initialCallsign, decimal? initialFrequencyMhz)
        : this(initialCallsign, initialFrequencyMhz, null)
    {
    }

    public LogInputWindow(string? initialCallsign, decimal? initialFrequencyMhz, string? initialSpotInfo)
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(LogInputWindow));
        App.Toasts.RegisterWindow(this);
        ApplyStayOnTopSetting();
        _viewModel = new LogInputViewModel();
        _viewModel.SetInitialSpot(initialCallsign, initialFrequencyMhz, initialSpotInfo);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _activeRigRefreshTimer.Tick += OnActiveRigRefreshTick;
        _activeRigRefreshTimer.Start();
        Closed += OnWindowClosed;
    }

    public void OnStampNowClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.StampNow();
    }

    public void OnAddDetailClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.AddDetail();
    }

    public void OnRemoveDetailClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.RemoveSelectedDetail();
    }

    public void OnUppercaseInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var current = textBox.Text ?? string.Empty;
        var upper = current.ToUpperInvariant();
        if (string.Equals(current, upper, StringComparison.Ordinal))
            return;

        var caret = textBox.CaretIndex;
        textBox.Text = upper;
        textBox.CaretIndex = Math.Min(caret, upper.Length);
    }

    public void OnGridFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (!ShouldHandleForwardTab(e) || !_viewModel.IsFieldDay)
            return;

        if (this.FindControl<TextBox>("FieldDayClassTextBox") is { IsVisible: true } classBox)
        {
            classBox.Focus();
            e.Handled = true;
        }
    }

    public void OnFieldDayClassKeyDown(object? sender, KeyEventArgs e)
    {
        if (!ShouldHandleForwardTab(e) || !_viewModel.IsFieldDay)
            return;

        if (this.FindControl<TextBox>("FieldDaySectionTextBox") is { IsVisible: true } sectionBox)
        {
            sectionBox.Focus();
            e.Handled = true;
        }
    }

    public void OnFieldDaySectionKeyDown(object? sender, KeyEventArgs e)
    {
        if (!ShouldHandleForwardTab(e) || !_viewModel.IsFieldDay)
            return;

        if (this.FindControl<Button>("LogQsoButton") is { IsVisible: true } logQsoButton)
        {
            logQsoButton.Focus();
            e.Handled = true;
        }
    }

    private static bool ShouldHandleForwardTab(KeyEventArgs e)
    {
        if (e.Key != Key.Tab)
            return false;

        return (e.KeyModifiers & KeyModifiers.Shift) == 0;
    }

    public void OnInputCallChanged(object? sender, TextChangedEventArgs e)
    {
        OnUppercaseInputChanged(sender, e);
        UpdateDuplicateStatusFromInputCall();
    }

    public void OnInputCallLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Ensure the VM has the final callsign value at blur time before duplicate checks run.
            _viewModel.InputCall = (textBox.Text ?? string.Empty).Trim().ToUpperInvariant();
        }

        UpdateDuplicateStatusFromInputCall();
    }

    private void UpdateDuplicateStatusFromInputCall()
    {
        if (_viewModel.InputCall.Trim().Length < 3)
        {
            ClearDuplicateStatusIfPresent();
            return;
        }

        if (_viewModel.TryGetDuplicateCallWarning(out var warning))
        {
            SetStatus(warning);
            return;
        }

        ClearDuplicateStatusIfPresent();
    }

    private void ClearDuplicateStatusIfPresent()
    {
        var label = this.FindControl<TextBlock>("StatusLabel");
        if (label is not null
            && !string.IsNullOrWhiteSpace(label.Text)
            && label.Text.StartsWith("Possible duplicate:", StringComparison.OrdinalIgnoreCase))
            label.Text = string.Empty;
    }

    public void OnApplySelectedRadioClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.EnableAutoRadioPopulate();
        _viewModel.ApplySelectedRadioToInputs();
    }

    public void OnLogQsoClicked(object? sender, RoutedEventArgs e)
    {
        var qso = _viewModel.TryBuildQso(out var error);
        if (qso is null)
        {
            SetStatus(error);
            App.Toasts.ShowError("QSO not logged", error);
            return;
        }

        QsoLogged?.Invoke(this, qso);
        _viewModel.PrepareForNextLogEntry();
        SetStatus("QSO logged.");
        App.Toasts.ShowSuccess("QSO logged", $"{qso.Call} on {qso.Band} {qso.Mode}");
    }

    public void OnCancelClicked(object? sender, RoutedEventArgs e) => Close();

    public void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.PrepareForNextLogEntry();
        SetStatus(string.Empty);
    }

    public async void OnSpotClicked(object? sender, RoutedEventArgs e)
    {
        await SubmitSpotAsync(isSelfSpot: false);
    }

    public async void OnSelfSpotClicked(object? sender, RoutedEventArgs e)
    {
        await SubmitSpotAsync(isSelfSpot: true);
    }

    public async void OnLookupClicked(object? sender, RoutedEventArgs e)
    {
        var config = AppConfigurationStore.Load();
        var service = CallsignLookupService.CreateDefault(config);
        var (result, errorMessage) = await service.LookupAsync(_viewModel.InputCall, CancellationToken.None);
        if (result is null)
        {
            var message = string.IsNullOrWhiteSpace(errorMessage) ? "Lookup failed." : errorMessage;
            SetStatus(message);
            App.Toasts.ShowWarning("Callsign lookup", message);
            return;
        }

        _viewModel.ApplyLookupResult(result);
        App.Toasts.ShowSuccess("Callsign lookup", $"Found {result.CallSign} via {result.Provider}.");
    }

    public void OnProgressStatusClicked(object? sender, RoutedEventArgs e)
    {
        var contest = _viewModel.CurrentContestDefinition;
        var key = contest.Key.Trim();
        var adif = contest.AdifContestId.Trim();
        var name = contest.DisplayName.Trim();

        if (_viewModel.IsFieldDay)
        {
            var window = App.FindOpenWindow<ArrlFdProgressWindow>() ?? new ArrlFdProgressWindow();
            ShowProgressWindow(window);
            return;
        }

        if (IsArqpContest(key, adif, name))
        {
            var window = App.FindOpenWindow<ArqpProgressWindow>() ?? new ArqpProgressWindow();
            ShowProgressWindow(window);
            return;
        }

        App.Toasts.ShowInfo("Progress status", "No contest progress window is available for the selected contest.");
    }

    private async Task SubmitSpotAsync(bool isSelfSpot)
    {
        var target = isSelfSpot ? _viewModel.StationCallSign : _viewModel.InputCall;
        var frequencyText = _viewModel.InputFreq?.Trim() ?? string.Empty;
        if (!decimal.TryParse(frequencyText, NumberStyles.Number, CultureInfo.InvariantCulture, out var mhz) || mhz <= 0)
        {
            App.Toasts.ShowError("DX cluster spot", "Enter a valid frequency in MHz before spotting.");
            return;
        }

        var comment = BuildSpotComment(isSelfSpot);
        var request = new DxSpotRequest(
            _viewModel.StationCallSign,
            target,
            mhz,
            comment,
            isSelfSpot);

        var result = await App.DxClusterSpotPublisher.SendSpotAsync(request);
        if (result.StartsWith("Spot sent", StringComparison.OrdinalIgnoreCase)
            || result.StartsWith("Self-spot sent", StringComparison.OrdinalIgnoreCase))
        {
            App.Toasts.ShowSuccess("DX cluster spot", result);
            return;
        }

        App.LogDxClusterNonSpot("SYS", $"Spot failed: {result}");
        App.Toasts.ShowError("DX cluster spot", result);
    }

    private string BuildSpotComment(bool isSelfSpot)
    {
        var remark = _viewModel.SpotRemark?.Trim() ?? string.Empty;
        return remark;
    }

    private void SetStatus(string message)
    {
        var label = this.FindControl<TextBlock>("StatusLabel");
        if (label != null)
            label.Text = message;
    }

    private void ShowProgressWindow(Window window)
    {
        if (window.IsVisible)
        {
            window.Activate();
            return;
        }

        if (this.IsVisible)
            window.Show(this);
        else
            window.Show();

        window.Activate();
    }

    private static bool IsArqpContest(string key, string adif, string name)
    {
        static bool IsArqpKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var upper = value.Trim().ToUpperInvariant();
            return upper is "ARQP" or "AR-QSO-PARTY";
        }

        return IsArqpKey(key)
               || IsArqpKey(adif)
               || name.Contains("Arkansas QSO Party", StringComparison.OrdinalIgnoreCase)
               || name.Contains("ARQP", StringComparison.OrdinalIgnoreCase);
    }

    private void OnActiveRigRefreshTick(object? sender, EventArgs e)
    {
        _viewModel.RefreshAutoFields();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
        CloseOpenProgressWindows();
        _activeRigRefreshTimer.Tick -= OnActiveRigRefreshTick;
        _activeRigRefreshTimer.Stop();
        Closed -= OnWindowClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LogInputViewModel.SelectedContestDefinition)
            or nameof(LogInputViewModel.CurrentContestDefinition)
            or nameof(LogInputViewModel.CurrentContestAdifId)
            or nameof(LogInputViewModel.IsFieldDay))
        {
            CloseOpenProgressWindows();
        }
    }

    private static void CloseOpenProgressWindows()
    {
        App.FindOpenWindow<ArqpProgressWindow>()?.Close();
        App.FindOpenWindow<ArrlFdProgressWindow>()?.Close();
    }

    private void ApplyStayOnTopSetting()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        Topmost = profile.StayOnTopLogInputWindow;

        var checkBox = this.FindControl<CheckBox>("StayOnTopCheckBox");
        if (checkBox is not null)
            checkBox.IsChecked = Topmost;
    }

    private void SaveStayOnTopSetting(bool isEnabled)
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        profile.StayOnTopLogInputWindow = isEnabled;
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
}
