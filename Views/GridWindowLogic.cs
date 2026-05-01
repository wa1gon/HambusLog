using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.Views;

public partial class GridWindow
{
    private GridViewModel? _viewModel;
    private SqliteQsoRepository? _repository;

    public GridWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(GridWindow));
        App.Toasts.RegisterWindow(this);
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
            App.Toasts.ShowSuccess("QSO saved", $"{qso.Call} logged on {qso.Band} {qso.Mode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QSO: {ex.Message}");
            App.Toasts.ShowError("Save failed", ex.Message);
        }
    }

    public void OnSortHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string column })
            _viewModel?.SortBy(column);
    }

    public async void OnGridRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || _repository is null)
            return;

        if (sender is not Border { DataContext: Qso rowQso })
            return;

        var fullQso = await _repository.GetByIdAsync(rowQso.Id);
        if (fullQso is null)
            return;

        var editor = new QsoEditWindow(fullQso);
        editor.QsoSaved += async (_, updated) => await SaveEditedQsoAsync(updated);
        _ = editor.ShowDialog(this);
    }

    private async Task SaveEditedQsoAsync(Qso updated)
    {
        if (_repository is null || _viewModel is null)
            return;

        try
        {
            await _repository.UpdateAsync(updated);
            await _repository.SaveChangesAsync();

            var existing = _viewModel.LogEntries.FirstOrDefault(x => x.Id == updated.Id);
            if (existing is null)
                return;

            existing.Call = updated.Call;
            existing.Band = updated.Band;
            existing.Mode = updated.Mode;
            existing.QsoDate = updated.QsoDate;
            existing.Freq = updated.Freq;
            existing.RstSent = updated.RstSent;
            existing.RstRcvd = updated.RstRcvd;
            existing.Details = updated.Details;

            var index = _viewModel.LogEntries.IndexOf(existing);
            _viewModel.LogEntries.RemoveAt(index);
            _viewModel.LogEntries.Insert(index, existing);

            App.Toasts.ShowSuccess("QSO updated", $"Changes saved for {updated.Call}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving edited QSO: {ex.Message}");
            App.Toasts.ShowError("Update failed", ex.Message);
        }
    }
}
