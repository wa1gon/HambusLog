namespace HamBusLog.Views;

using Avalonia.Input;
using Avalonia.Platform;

public partial class SplashWindow : Window
{
    private const int SplashSeconds = 30;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>The screen the splash was displayed on — read by AppLogic after Close().</summary>
    public Screen? HostScreen { get; private set; }

    /// <summary>The final splash top-left position (pixels), used to place main window on the same monitor.</summary>
    public PixelPoint LastKnownPosition { get; private set; }

    public SplashWindow()
    {
        InitializeComponent();
        SetVersionInfo();
    }

    private void SetVersionInfo()
    {
        var versionText = this.FindControl<TextBlock>("VersionText");
        var buildText = this.FindControl<TextBlock>("BuildText");

        if (versionText is not null)
            versionText.Text = $"v{AppVersionService.Version}";

        if (buildText is not null)
            buildText.Text = $"build {AppVersionService.BuildNumber}";
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        HostScreen = Screens.ScreenFromWindow(this);
        LastKnownPosition = Position;
        _ = RunCountdownAsync(_cts.Token);
    }

    private async Task RunCountdownAsync(CancellationToken ct)
    {
        var countdownText = this.FindControl<TextBlock>("CountdownText");

        for (var remaining = SplashSeconds; remaining > 0; remaining--)
        {
            if (ct.IsCancellationRequested)
                return;

            if (countdownText is not null)
                countdownText.Text = $"Closing in {remaining} s\u2026";

            try
            {
                await Task.Delay(1000, ct);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        if (!ct.IsCancellationRequested)
            Close();
    }

    private void OnDismissClicked(object? sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts.Cancel();
        _cts.Dispose();
        base.OnClosed(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        LastKnownPosition = Position;
        HostScreen = Screens.ScreenFromPoint(LastKnownPosition)
                     ?? Screens.ScreenFromWindow(this)
                     ?? HostScreen;
        base.OnClosing(e);
    }

    private void OnSplashPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        // Don't hijack button clicks; only start drag from non-button surfaces.
        if (e.Source is Button)
            return;

        BeginMoveDrag(e);
    }
}










