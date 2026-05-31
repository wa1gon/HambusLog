namespace HamBusLog.Views;

using Avalonia.Controls;
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
        ApplyStayOnTopSetting();
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

    private void ApplyStayOnTopSetting()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        Topmost = profile.StayOnTopArqpProgressWindow;

        var checkBox = this.FindControl<CheckBox>("StayOnTopCheckBox");
        if (checkBox is not null)
            checkBox.IsChecked = Topmost;
    }

    private void SaveStayOnTopSetting(bool isEnabled)
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        profile.StayOnTopArqpProgressWindow = isEnabled;
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
}
