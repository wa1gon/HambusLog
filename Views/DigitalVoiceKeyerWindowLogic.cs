namespace HamBusLog.Views;

public partial class DigitalVoiceKeyerWindow
{
    private readonly DigitalVoiceKeyerViewModel _viewModel;
    private CancellationTokenSource? _activePlaybackCts;
    private DigitalVoiceKeyerRowViewModel? _activePlaybackRow;

    public DigitalVoiceKeyerWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(DigitalVoiceKeyerWindow));
        _viewModel = new DigitalVoiceKeyerViewModel();
        DataContext = _viewModel;
        ApplyStayOnTopSetting();
    }

    protected override void OnClosed(EventArgs e)
    {
        StopPlayback("Playback stopped.");
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            StopPlayback("Playback aborted (Esc).");
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnReloadClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.ReloadCurrentBank();
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        StopPlayback("Playback stopped.");
        _viewModel.SaveCurrentBank();
    }

    private void ApplyStayOnTopSetting()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        Topmost = profile.StayOnTopDigitalVoiceKeyerWindow;

        var checkBox = this.FindControl<CheckBox>("StayOnTopCheckBox");
        if (checkBox is not null)
            checkBox.IsChecked = Topmost;
    }

    private void SaveStayOnTopSetting(bool isEnabled)
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        profile.StayOnTopDigitalVoiceKeyerWindow = isEnabled;
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

    private async void OnSlotRecordButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        var slotNumber = ReadSlotNumber(button.Tag);
        if (slotNumber <= 0)
            return;

        await _viewModel.RecordSlotAsync(slotNumber);
    }

    private async void OnSlotPlayButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not DigitalVoiceKeyerRowViewModel row)
            return;

        var slotNumber = ReadSlotNumber(button.Tag);
        if (slotNumber <= 0)
            return;

        if (row.IsPlaying)
        {
            StopPlayback($"Stopped playback for slot {slotNumber}.");
            return;
        }

        StopPlayback(null);
        row.IsPlaying = true;
        _activePlaybackRow = row;
        _activePlaybackCts = new CancellationTokenSource();
        _ = RunPlayLoopAsync(slotNumber, row, _activePlaybackCts.Token);
    }

    private void OnSlotDeleteButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        var slotNumber = ReadSlotNumber(button.Tag);
        if (slotNumber <= 0)
            return;

        if (_activePlaybackRow?.SlotNumber == slotNumber)
            StopPlayback($"Stopped playback for slot {slotNumber}.");

        _viewModel.DeleteRecording(slotNumber);
    }

    private async Task RunPlayLoopAsync(int slotNumber, DigitalVoiceKeyerRowViewModel row, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _viewModel.PlaySlotAsync(slotNumber, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !result.Success)
                    break;

                var repeatDelaySeconds = Math.Max(0, row.RepeatDelaySeconds);
                if (repeatDelaySeconds <= 0)
                    break;

                _viewModel.SetStatusMessage($"Repeating slot {slotNumber} in {repeatDelaySeconds}s. Press Esc or click Stop to abort.");
                await Task.Delay(TimeSpan.FromSeconds(repeatDelaySeconds), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            row.IsPlaying = false;
            if (ReferenceEquals(_activePlaybackRow, row))
            {
                _activePlaybackCts?.Dispose();
                _activePlaybackCts = null;
                _activePlaybackRow = null;
            }
        }
    }

    private void StopPlayback(string? statusMessage)
    {
        var cts = _activePlaybackCts;
        var row = _activePlaybackRow;
        _activePlaybackCts = null;
        _activePlaybackRow = null;

        if (cts is not null)
        {
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

        if (row is not null)
            row.IsPlaying = false;

        if (!string.IsNullOrWhiteSpace(statusMessage))
            _viewModel.SetStatusMessage(statusMessage);
    }

    private static int ReadSlotNumber(object? tag)
    {
        return tag switch
        {
            int slot => slot,
            string slotText when int.TryParse(slotText, out var parsed) => parsed,
            _ => 0
        };
    }
}







