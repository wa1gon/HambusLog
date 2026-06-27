namespace HamBusLog.Views;

public partial class DigitalVoiceKeyerWindow
{
    private readonly DigitalVoiceKeyerViewModel _viewModel;

    public DigitalVoiceKeyerWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(DigitalVoiceKeyerWindow));
        _viewModel = new DigitalVoiceKeyerViewModel();
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void OnReloadClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.ReloadCurrentBank();
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.SaveCurrentBank();
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

        var slotNumber = ReadSlotNumber(button.Tag);
        if (slotNumber <= 0)
            return;

        await _viewModel.PlaySlotAsync(slotNumber);
    }

    private void OnSlotDeleteButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        var slotNumber = ReadSlotNumber(button.Tag);
        if (slotNumber <= 0)
            return;

        _viewModel.DeleteRecording(slotNumber);
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




