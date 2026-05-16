using Avalonia.Threading;
using HamBusLog.Wa1gonLib.Models;

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
        _viewModel = new LogInputViewModel();
        _viewModel.SetInitialSpot(initialCallsign, initialFrequencyMhz, initialSpotInfo);
        DataContext = _viewModel;

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

    public async void OnSpotClicked(object? sender, RoutedEventArgs e)
    {
        await SubmitSpotAsync(isSelfSpot: false);
    }

    public async void OnSelfSpotClicked(object? sender, RoutedEventArgs e)
    {
        await SubmitSpotAsync(isSelfSpot: true);
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

        App.Toasts.ShowError("DX cluster spot", result);
    }

    private string BuildSpotComment(bool isSelfSpot)
    {
        var mode = _viewModel.InputMode?.Trim().ToUpperInvariant() ?? string.Empty;
        var band = _viewModel.InputBand?.Trim().ToUpperInvariant() ?? string.Empty;
        var tag = isSelfSpot ? "SELF-SPOT" : "SPOT";

        var parts = new List<string> { tag };
        if (!string.IsNullOrWhiteSpace(mode))
            parts.Add(mode);
        if (!string.IsNullOrWhiteSpace(band))
            parts.Add(band);

        return string.Join(" ", parts);
    }

    private void SetStatus(string message)
    {
        var label = this.FindControl<TextBlock>("StatusLabel");
        if (label != null)
            label.Text = message;
    }

    private void OnActiveRigRefreshTick(object? sender, EventArgs e)
    {
        _viewModel.RefreshAutoFields();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _activeRigRefreshTimer.Tick -= OnActiveRigRefreshTick;
        _activeRigRefreshTimer.Stop();
        Closed -= OnWindowClosed;
    }
}
