using Avalonia.Controls;
using Avalonia.Interactivity;
using HamBusLog.ViewModels;

namespace HamBusLog.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
    }

    public void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.Save();
    }

    public void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

