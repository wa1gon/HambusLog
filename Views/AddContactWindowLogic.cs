namespace HamBusLog.Views;

using HamBusLog.ViewModels;
using HamBusLog.Data;
using HamBusLog.Services;

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

    public async void OnLookupClicked(object? sender, RoutedEventArgs e)
    {
        var config = AppConfigurationStore.Load();
        var service = CallsignLookupService.CreateDefault(config);
        var (result, errorMessage) = await service.LookupAsync(_viewModel.InputCall, CancellationToken.None);
        if (result is null)
        {
            var message = string.IsNullOrWhiteSpace(errorMessage) ? "Lookup failed." : errorMessage;
            App.Toasts.ShowWarning("Callsign lookup", message);
            return;
        }

        _viewModel.ApplyLookupResult(result);
        App.Toasts.ShowSuccess("Callsign lookup", $"Found {result.CallSign} via {result.Provider}.");
    }
}