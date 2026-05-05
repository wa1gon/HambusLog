namespace HamBusLog.Views;

public partial class DxSpotsWindow
{
    private readonly DxSpotsWindowViewModel _viewModel;
    private readonly SqliteQsoRepository _repository;
    private LogInputWindow? _logInputWindow;

    public DxSpotsWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(DxSpotsWindow));
        _viewModel = new DxSpotsWindowViewModel();
        _repository = new SqliteQsoRepository(App.DbContext);
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private async void OnSpotsGridTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not DataGrid { SelectedItem: DxSpot spot })
            return;

        var mhz = ConvertSpotFrequencyToMhz(spot.Frequency);
        if (mhz <= 0)
        {
            App.Toasts.ShowWarning("DX Cluster", "Selected spot has an invalid frequency.");
            return;
        }

        var (state, requestedRadioName, usedFallback) = ResolveTargetActiveRadio();
        if (string.IsNullOrWhiteSpace(requestedRadioName))
        {
            App.Toasts.ShowWarning("DX Cluster", "No active radio is configured for tuning.");
            return;
        }

        if (state is null || !state.IsConnected)
        {
            App.Toasts.ShowWarning("DX Cluster", $"Active radio '{requestedRadioName}' is not connected.");
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var frequencyResult = await App.RigctldConnectionManager.SetFrequencyByNameAsync(state.RadioName, mhz, cts.Token);
            var modeToApply = DeriveRigMode(spot.Info, mhz);
            var modeResult = string.Empty;
            if (!string.IsNullOrWhiteSpace(modeToApply))
                modeResult = await App.RigctldConnectionManager.SetModeByNameAsync(state.RadioName, modeToApply, cts.Token);

            var station = string.IsNullOrWhiteSpace(spot.Callsign) ? "(unknown)" : spot.Callsign;
            if (usedFallback)
                App.Toasts.ShowInfo("DX Cluster", $"Primary active radio '{requestedRadioName}' was unavailable, tuned '{state.RadioName}' instead.");
            var modeMessage = string.IsNullOrWhiteSpace(modeToApply)
                ? ""
                : $" Mode: {modeToApply}. {modeResult}";
            App.Toasts.ShowSuccess("DX Cluster", $"Tuned {state.RadioName} to {mhz:0.000} MHz for {station}. {frequencyResult}{modeMessage}");
            OpenLogInputWindowForSpot(spot.Callsign, mhz, spot.Info);
        }
        catch (TimeoutException)
        {
            App.Toasts.ShowError("DX Cluster", "Frequency update timed out. Verify radio connection and try again.");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("DX Cluster", "Frequency update failed: " + ex.Message);
        }
    }

    private static decimal ConvertSpotFrequencyToMhz(double spotFrequency)
    {
        if (spotFrequency <= 0)
            return 0;

        var mhz = spotFrequency >= 1000 ? spotFrequency / 1000d : spotFrequency;
        return Math.Round((decimal)mhz, 3, MidpointRounding.AwayFromZero);
    }

    private static string DeriveRigMode(string? spotInfo, decimal mhz)
    {
        if (!string.IsNullOrWhiteSpace(spotInfo))
        {
            var text = spotInfo.Trim().ToUpperInvariant();
            if (Regex.IsMatch(text, @"\bCW\b")) return "CW";
            if (Regex.IsMatch(text, @"\bRTTY\b")) return "RTTY";
            if (Regex.IsMatch(text, @"\bFT8\b|\bFT4\b|\bPSK\d*\b|\bDIGU\b")) return mhz < 10m ? "DIGL" : "DIGU";
            if (Regex.IsMatch(text, @"\bDIGL\b")) return "DIGL";
            if (Regex.IsMatch(text, @"\bUSB\b")) return "USB";
            if (Regex.IsMatch(text, @"\bLSB\b")) return "LSB";
            if (Regex.IsMatch(text, @"\bAM\b")) return "AM";
            if (Regex.IsMatch(text, @"\bFM\b")) return "FM";
            if (Regex.IsMatch(text, @"\bSSB\b")) return mhz < 10m ? "LSB" : "USB";
        }

        if (mhz >= 50m)
            return "FM";
        if (mhz < 10m)
            return "LSB";
        return "USB";
    }

    private (RadioRuntimeState? State, string RequestedRadioName, bool UsedFallback) ResolveTargetActiveRadio()
    {
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);
        var snapshot = App.RigctldConnectionManager.GetSnapshot();

        var activeRadioNames = rigctld.ActiveRadioNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (activeRadioNames.Count == 0 && !string.IsNullOrWhiteSpace(rigctld.ActiveRadioName))
            activeRadioNames.Add(rigctld.ActiveRadioName.Trim());

        var requested = activeRadioNames.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requested))
            return (null, string.Empty, false);

        var requestedState = snapshot.FirstOrDefault(x =>
            string.Equals(x.RadioName, requested, StringComparison.OrdinalIgnoreCase));
        if (requestedState is { IsConnected: true })
            return (requestedState, requested, false);

        foreach (var candidate in activeRadioNames.Skip(1))
        {
            var candidateState = snapshot.FirstOrDefault(x =>
                string.Equals(x.RadioName, candidate, StringComparison.OrdinalIgnoreCase));
            if (candidateState is { IsConnected: true })
                return (candidateState, requested, true);
        }

        return (requestedState, requested, false);
    }

    private void OpenLogInputWindowForSpot(string? callsign, decimal frequencyMhz, string? spotInfo)
    {
        if (_logInputWindow is { IsVisible: true })
        {
            _logInputWindow.Activate();
            return;
        }

        if (App.ActivateOpenWindow<LogInputWindow>())
            return;

        _logInputWindow = new LogInputWindow(callsign, frequencyMhz, spotInfo);
        _logInputWindow.Closed += (_, _) => _logInputWindow = null;
        _logInputWindow.QsoLogged += async (_, qso) => await SaveQsoAsync(qso);
        _logInputWindow.Show();
        _logInputWindow.Activate();
    }

    private async Task SaveQsoAsync(Qso qso)
    {
        try
        {
            await _repository.AddAsync(qso);
            await _repository.SaveChangesAsync();
            App.Toasts.ShowSuccess("QSO saved", $"{qso.Call} logged on {qso.Band} {qso.Mode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QSO: {ex.Message}");
            App.Toasts.ShowError("Save failed", ex.Message);
        }
    }
}
