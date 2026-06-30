namespace HamBusLog.Views;

using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

public partial class SplashWindow : Window
{
    private const int SplashSeconds = 30;
    private const string SplashImageRelativePath = "images/hambus-large.png";
    private readonly CancellationTokenSource _cts = new();
    private bool _isClosingOrClosed;

    /// <summary>The screen the splash was displayed on — read by AppLogic after Close().</summary>
    public Screen? HostScreen { get; private set; }

    /// <summary>The final splash top-left position (pixels), used to place main window on the same monitor.</summary>
    public PixelPoint LastKnownPosition { get; private set; }

    public SplashWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerPressedEvent, OnAnyPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        SetSplashImage();
        SetVersionInfo();
    }

    private void SetSplashImage()
    {
        var splashLogo = this.FindControl<Image>("SplashLogo");
        if (splashLogo is null)
            return;

        var imagePath = Path.Combine(AppContext.BaseDirectory, SplashImageRelativePath);
        if (!File.Exists(imagePath))
            return;

        try
        {
            using var stream = File.OpenRead(imagePath);
            splashLogo.Source = new Bitmap(stream);
        }
        catch
        {
            // Keep the embedded image fallback if the external file can't be loaded.
        }
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

    protected override void OnClosed(EventArgs e)
    {
        _isClosingOrClosed = true;
        RemoveHandler(InputElement.PointerPressedEvent, OnAnyPointerPressed);
        TryCancelCountdown();
        _cts.Dispose();
        base.OnClosed(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _isClosingOrClosed = true;
        LastKnownPosition = Position;
        HostScreen = Screens.ScreenFromPoint(LastKnownPosition)
                     ?? Screens.ScreenFromWindow(this)
                     ?? HostScreen;
        base.OnClosing(e);
    }

    private void OnAnyPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isClosingOrClosed)
            return;

        var point = e.GetCurrentPoint(this);
        if (point.Pointer.Type != PointerType.Mouse)
            return;

        _isClosingOrClosed = true;
        e.Handled = true;  // Mark as handled so event doesn't tunnel to main window.
        // Any mouse click dismisses the splash immediately.
        TryCancelCountdown();
        Close();
    }

    private void TryCancelCountdown()
    {
        try
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ignore races from late UI events after splash teardown.
        }
    }
}












