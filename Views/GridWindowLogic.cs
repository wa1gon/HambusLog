namespace HamBusLog.Views;

public partial class GridWindow
{
    private GridViewModel? _viewModel;

    public GridWindow()
    {
        InitializeComponent();
        _viewModel = new GridViewModel();
        DataContext = _viewModel;
    }

    public void OnNewEntryClicked(object? sender, RoutedEventArgs e)
    {
        OpenLogInputWindow();
    }

    public void OpenLogInputWindow()
    {
        if (_viewModel is null)
            return;

        var inputWindow = new LogInputWindow();
        inputWindow.QsoLogged += (_, qso) => _viewModel.LogEntries.Add(qso);
        inputWindow.Show(this);
    }

    public void OnSortHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string column })
            _viewModel?.SortBy(column);
    }
}

