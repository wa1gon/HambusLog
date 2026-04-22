using Avalonia.Controls;
using System;
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
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateUIForContestType();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GridViewModel.SelectedContestType))
        {
            UpdateUIForContestType();
        }
    }

    private void UpdateUIForContestType()
    {
        if (_viewModel == null) return;
        
        var modeLabel = this.FindControl<TextBlock>("ContestModeLabel");
        var fieldDayPanel = this.FindControl<StackPanel>("FieldDayExchange");
        var sentRstPanel = this.FindControl<StackPanel>("SentRstPanel");
        var recRstPanel = this.FindControl<StackPanel>("RecRstPanel");
        var isFieldDay = _viewModel.SelectedContestType == HamBusLog.ViewModels.ContestType.ArrlFieldDay;
        
        if (modeLabel != null)
        {
            modeLabel.Text = isFieldDay
                ? "Mode: ARRL Field Day"
                : "Mode: Normal QSO";
        }
        
        if (fieldDayPanel != null)
        {
            fieldDayPanel.IsVisible = isFieldDay;
        }

        if (sentRstPanel != null)
        {
            sentRstPanel.IsVisible = !isFieldDay;
        }

        if (recRstPanel != null)
        {
            recRstPanel.IsVisible = !isFieldDay;
        }
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


