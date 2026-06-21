namespace HamBusLog.Views;

using HamBusLog.ViewModels;

public partial class FieldDayBonusPointsWindow : Window
{
    public FieldDayBonusPointsWindow()
    {
        InitializeComponent();
        DataContext = new FieldDayBonusPointsViewModel();
    }

    private FieldDayBonusPointsViewModel ViewModel => (FieldDayBonusPointsViewModel)DataContext!;

    public int BonusPoints { get; private set; }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        BonusPoints = ViewModel.GetTotalBonusPoints();
        Close(true);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        BonusPoints = 0;
        Close(false);
    }
}

