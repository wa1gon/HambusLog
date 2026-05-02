namespace HamBusLog.Views;

public partial class DxSpotsWindow
{
    private readonly DxSpotsWindowViewModel _viewModel;

    public DxSpotsWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(DxSpotsWindow));
        _viewModel = new DxSpotsWindowViewModel();
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}

