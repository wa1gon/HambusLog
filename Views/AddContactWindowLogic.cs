namespace HamBusLog.Views;

using HamBusLog.ViewModels;

public partial class AddContactWindow
{
    private readonly AddContactViewModel _viewModel;

    public AddContactWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(AddContactWindow));
        App.Toasts.RegisterWindow(this);
        _viewModel = new AddContactViewModel();
        DataContext = _viewModel;
    }

    private void OnStayOnTopChecked(object? sender, RoutedEventArgs e) => Topmost = true;

    private void OnStayOnTopUnchecked(object? sender, RoutedEventArgs e) => Topmost = false;
}