using Avalonia.Threading;
using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.Views;

public partial class LogInputWindow
{
    private readonly LogInputViewModel _viewModel;
    private readonly DispatcherTimer _activeRigRefreshTimer = new() { Interval = TimeSpan.FromSeconds(0.5) };

    /// <summary>Raised when the user successfully logs a QSO.</summary>
    public event EventHandler<Qso>? QsoLogged;

    public LogInputWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(LogInputWindow));
        _viewModel = new LogInputViewModel();
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
        _viewModel.ApplySelectedRadioToInputs();
    }

    public void OnLogQsoClicked(object? sender, RoutedEventArgs e)
    {
        var qso = _viewModel.TryBuildQso(out var error);
        if (qso is null)
        {
            SetStatus(error);
            return;
        }

        QsoLogged?.Invoke(this, qso);
        _viewModel.PrepareForNextLogEntry();
        SetStatus("QSO logged.");
    }

    public void OnCancelClicked(object? sender, RoutedEventArgs e) => Close();

    private void SetStatus(string message)
    {
        var label = this.FindControl<TextBlock>("StatusLabel");
        if (label != null)
            label.Text = message;
    }

    private void OnActiveRigRefreshTick(object? sender, EventArgs e)
    {
        _viewModel.RefreshSelectedRadioInputs();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _activeRigRefreshTimer.Tick -= OnActiveRigRefreshTick;
        _activeRigRefreshTimer.Stop();
        Closed -= OnWindowClosed;
    }
}
