namespace HamBusLog.Views;

public partial class WsjtDebugWindow
{
    private readonly WsjtDebugWindowViewModel _viewModel;

    public WsjtDebugWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(WsjtDebugWindow));
        App.Toasts.RegisterWindow(this);
        _viewModel = new WsjtDebugWindowViewModel();
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.Clear();
    }
}

