namespace HamBusLog.Views;

using Avalonia.Threading;

public partial class ArqpProgressWindow : Window
{
    public ArqpProgressWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(ArqpProgressWindow));
        DataContext = new ArqpProgressViewModel();
        App.DbContextReinitialized += OnDbContextReinitialized;
        App.QsoSaved += OnQsoSaved;
        Activated += OnWindowActivated;
        ViewModel.Refresh();
    }

    private ArqpProgressViewModel ViewModel => (ArqpProgressViewModel)DataContext!;

    protected override void OnClosed(EventArgs e)
    {
        App.DbContextReinitialized -= OnDbContextReinitialized;
        App.QsoSaved -= OnQsoSaved;
        Activated -= OnWindowActivated;
        base.OnClosed(e);
    }

    private void OnDbContextReinitialized(object? sender, EventArgs e)
    {
        ViewModel.Refresh();
    }

    private void OnQsoSaved(object? sender, Qso qso)
    {
        Dispatcher.UIThread.Post(ViewModel.Refresh);
    }

    private void OnRefreshClicked(object? sender, RoutedEventArgs e)
    {
        ViewModel.Refresh();
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        ViewModel.Refresh();
    }
}




