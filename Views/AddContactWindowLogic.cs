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
        Content = new AddContactView { DataContext = _viewModel };
    }
}