namespace HamBusLog.Views;

public partial class GridWindow
{
    private GridViewModel? _viewModel;
    private SqliteQsoRepository? _repository;

    public GridWindow()
    {
        InitializeComponent();
        _repository = new SqliteQsoRepository(App.DbContext);
        _viewModel = new GridViewModel(_repository);
        DataContext = _viewModel;
    }

    public void OnNewEntryClicked(object? sender, RoutedEventArgs e)
    {
        OpenLogInputWindow();
    }

    public void OpenLogInputWindow()
    {
        if (_viewModel is null || _repository is null)
            return;

        var inputWindow = new LogInputWindow();
        inputWindow.QsoLogged += async (_, qso) => 
        {
            _viewModel.LogEntries.Add(qso);
            // Save to database
            await SaveQsoAsync(qso);
        };
        inputWindow.Show(this);
    }
    
    private async Task SaveQsoAsync(Qso qso)
    {
        try
        {
            if (_repository is null)
                return;
                
            await _repository.AddAsync(qso);
            await _repository.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"Saved QSO: {qso.Call}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QSO: {ex.Message}");
        }
    }

    public void OnSortHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string column })
            _viewModel?.SortBy(column);
    }
}

