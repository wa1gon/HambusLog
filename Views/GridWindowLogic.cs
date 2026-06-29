using Avalonia.Threading;
using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.Views;

public partial class GridWindow
{
    private GridViewModel? _viewModel;
    private SqliteQsoRepository? _repository;
    private LogInputWindow? _logInputWindow;
    private QsoEditWindow? _qsoEditWindow;

    public GridWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(GridWindow));
        App.Toasts.RegisterWindow(this);
        RebuildRepositoryBinding();
        App.DbContextReinitialized += OnDbContextReinitialized;
        App.QsoSaved += OnQsoSaved;
        App.LogTypeSelectionService.SelectedContestChanged += OnSelectedContestChanged;
        // Defer column visibility until window is loaded
        this.Loaded += (_, _) => ApplyContestColumns();
    }

    protected override void OnClosed(EventArgs e)
    {
        App.DbContextReinitialized -= OnDbContextReinitialized;
        App.QsoSaved -= OnQsoSaved;
        App.LogTypeSelectionService.SelectedContestChanged -= OnSelectedContestChanged;
        base.OnClosed(e);
    }

    private void OnSelectedContestChanged(object? sender, EventArgs e)
    {
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            ApplyContestColumns();
            return;
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(ApplyContestColumns);
    }

    private void ApplyContestColumns()
    {
        var contest = App.LogTypeSelectionService.GetSelectedContestDefinition();
        var showFieldDayColumns = contest.UsesFieldDayExchange;

        System.Diagnostics.Debug.WriteLine($"ApplyContestColumns: contest={contest.Key}, usesFieldDay={showFieldDayColumns}, totalColumns={QsoDataGrid.Columns.Count}");

        // Search all columns for Section and Class
        DataGridColumn? sectionColumn = null;
        DataGridColumn? classColumn = null;

        foreach (var col in QsoDataGrid.Columns)
        {
            var headerText = col.Header?.ToString() ?? string.Empty;
            if (string.Equals(headerText, "Section", StringComparison.OrdinalIgnoreCase))
                sectionColumn = col;
            else if (string.Equals(headerText, "Class", StringComparison.OrdinalIgnoreCase))
                classColumn = col;
        }

        System.Diagnostics.Debug.WriteLine($"  Found Section column: {sectionColumn is not null}, Class column: {classColumn is not null}");

        if (sectionColumn is not null)
            sectionColumn.IsVisible = showFieldDayColumns;
        if (classColumn is not null)
            classColumn.IsVisible = showFieldDayColumns;
    }

    private void OnQsoSaved(object? sender, Qso qso)
    {
        if (_viewModel is null)
            return;

        // Already on UI thread if triggered from the manual log flow; WSJT comes from a
        // background task so marshal either way to be safe.
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            UpsertQso(qso);
            return;
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(() => UpsertQso(qso));
    }

    private void UpsertQso(Qso qso)
    {
        if (_viewModel is null)
            return;

        var existingIndex = _viewModel.LogEntries
            .Select((item, index) => new { item, index })
            .FirstOrDefault(x => x.item.Id == qso.Id)
            ?.index;

        if (existingIndex is not null)
        {
            _viewModel.LogEntries[(int)existingIndex] = qso;
            return;
        }

        _viewModel.LogEntries.Insert(0, qso);
    }

    private void OnDbContextReinitialized(object? sender, EventArgs e)
    {
        RebuildRepositoryBinding();
        App.Toasts.ShowInfo("Database", "Database connection switched to the updated SQLite file.");
    }

    private void RebuildRepositoryBinding()
    {
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

        if (_logInputWindow is { IsVisible: true })
        {
            _logInputWindow.Activate();
            return;
        }

        var existingWindow = App.FindOpenWindow<LogInputWindow>();
        if (existingWindow is not null)
        {
            _logInputWindow = existingWindow;
            ShowLogInputWindow(existingWindow);
            return;
        }

        _logInputWindow = new LogInputWindow();
        _logInputWindow.Closed += (_, _) => _logInputWindow = null;
        _logInputWindow.QsoLogged += async (_, qso) => 
        {
            _viewModel.LogEntries.Insert(0, qso);
            // Save to database
            await SaveQsoAsync(qso);
        };
        ShowLogInputWindow(_logInputWindow);
    }

    private void ShowLogInputWindow(LogInputWindow window)
    {
        if (window.IsVisible)
        {
            window.Activate();
            return;
        }

        if (IsVisible)
            window.Show(this);
        else
            window.Show();

        Dispatcher.UIThread.Post(window.Activate);
    }
    
    private async Task SaveQsoAsync(Qso qso)
    {
        try
        {
            if (_repository is null)
                return;

            await _repository.AddAsync(qso);
            await _repository.SaveChangesAsync();
            App.RaiseQsoSaved(qso);
            System.Diagnostics.Debug.WriteLine($"Saved QSO: {qso.Call}");
            App.Toasts.ShowSuccess("QSO saved", $"{qso.Call} logged on {qso.Band} {qso.Mode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QSO: {ex.Message}");
            App.Toasts.ShowError("Save failed", ex.Message);
        }
    }

    public async void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel is null || _repository is null)
            return;

        if (sender is not DataGrid { SelectedItem: Qso rowQso })
            return;

        var fullQso = await _repository.GetByIdAsync(rowQso.Id);
        if (fullQso is null)
            return;

        if (_qsoEditWindow is { IsVisible: true })
        {
            _qsoEditWindow.Activate();
            return;
        }

        if (App.ActivateOpenWindow<QsoEditWindow>())
            return;

        _qsoEditWindow = new QsoEditWindow(fullQso);
        _qsoEditWindow.Closed += (_, _) => _qsoEditWindow = null;
        _qsoEditWindow.QsoSaved += async (_, updated) => await SaveEditedQsoAsync(updated);
        _ = _qsoEditWindow.ShowDialog(this);
    }

    private async Task SaveEditedQsoAsync(Qso updated)
    {
        if (_repository is null || _viewModel is null)
            return;

        try
        {
            await _repository.UpdateAsync(updated);
            await _repository.SaveChangesAsync();
            App.RaiseQsoSaved(updated);

            App.Toasts.ShowSuccess("QSO updated", $"Changes saved for {updated.Call}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving edited QSO: {ex.Message}");
            App.Toasts.ShowError("Update failed", ex.Message);
        }
    }

    public async void OnDeleteRowClicked(object? sender, RoutedEventArgs e)
    {
        if (_repository is null || _viewModel is null)
            return;

        if (sender is not Button { DataContext: Qso qso })
            return;

        try
        {
            await _repository.DeleteAsync(qso.Id);
            await _repository.SaveChangesAsync();

            var inMemoryQso = _viewModel.LogEntries.FirstOrDefault(x => x.Id == qso.Id);
            if (inMemoryQso is not null)
                _viewModel.LogEntries.Remove(inMemoryQso);

            App.Toasts.ShowSuccess("QSO deleted", $"Removed {qso.Call}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting QSO: {ex.Message}");
            App.Toasts.ShowError("Delete failed", ex.Message);
        }
    }
}
