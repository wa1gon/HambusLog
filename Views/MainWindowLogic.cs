namespace HamBusLog.Views;

public partial class MainWindow
{
    private MenuNode? _previousSelection;
    private GridWindow? _gridWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    public async void OnMenuTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuNode node)
        {
            if (node.Title == "Grid" || node.Title == "Open/Reopen Grid")
            {
                ToggleGridWindow();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Add New Contact")
            {
                OpenNewContactWindow();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Configuration")
            {
                OpenConfigurationWindow();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Import ADIF")
            {
                await ImportAdifAsync();
                ResetTreeSelection(sender);
            }
            else
            {
                _previousSelection = node;
            }
        }
    }

    private void ToggleGridWindow()
    {
        if (_gridWindow is { IsVisible: true })
        {
            _gridWindow.Hide();
            return;
        }

        if (_gridWindow is null)
        {
            _gridWindow = new GridWindow();
            _gridWindow.Closed += (_, _) => _gridWindow = null;
        }

        ShowWithVisibleOwner(_gridWindow);
    }

    private void OpenConfigurationWindow()
    {
        var configurationWindow = new ConfigurationWindow();
        ShowWithVisibleOwner(configurationWindow);
    }

    private void OpenNewContactWindow()
    {
        EnsureGridWindowVisible();
        _gridWindow?.OpenLogInputWindow();
    }

    private void EnsureGridWindowVisible()
    {
        if (_gridWindow is null)
        {
            _gridWindow = new GridWindow();
            _gridWindow.Closed += (_, _) => _gridWindow = null;
        }

        if (!_gridWindow.IsVisible)
            ShowWithVisibleOwner(_gridWindow);
    }

    private void ShowWithVisibleOwner(Window window)
    {
        if (IsVisible)
        {
            window.Show(this);
            return;
        }

        window.Show();
    }

    private void ResetTreeSelection(object? sender)
    {
        if (_previousSelection != null && sender is TreeView tv)
            tv.SelectedItem = _previousSelection;
    }

    private async Task ImportAdifAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select ADIF file to import",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("ADIF files") { Patterns = ["*.adi", "*.adif"] },
                new Avalonia.Platform.Storage.FilePickerFileType("All files") { Patterns = ["*"] }
            ]
        });

        if (files.Count == 0)
            return;

        var path = files[0].Path.LocalPath;
        try
        {
            var result = await AdifImportService.ImportFromFileAsync(path);
            await ShowMessageAsync(
                "ADIF Import Complete",
                $"Imported {result.ParsedCount} QSO record(s) from:\n{result.FilePath}\n\nDatabase change count: {result.SavedChanges}");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("ADIF Import Failed", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var ok = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            MinWidth = 80
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(14),
            Spacing = 8,
            Children = { text, ok }
        };

        var dialog = new Window
        {
            Width = 520,
            Height = 220,
            MinWidth = 420,
            MinHeight = 180,
            Title = title,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        ok.Click += (_, _) => dialog.Close();

        if (IsVisible)
            await dialog.ShowDialog(this);
        else
            dialog.Show();
    }
}




