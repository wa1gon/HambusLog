using Avalonia.Controls;
using Avalonia.Interactivity;
using HamBusLog.ViewModels;

namespace HamBusLog.Views;

public partial class GridWindow : Window
{
    private GridViewModel? _viewModel;

    public GridWindow()
    {
        InitializeComponent();
        _viewModel = new GridViewModel();
        DataContext = _viewModel;
    }

    public void OnAddEntryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel?.AddNewEntry();
    }

    public void OnSortHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string column })
        {
            _viewModel?.SortBy(column);
        }
    }
}


