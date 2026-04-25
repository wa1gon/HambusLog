namespace HamBusLog.Views;

public partial class MainWindow
{
    private MenuNode? _previousSelection;
    private GridWindow? _gridWindow;
    private bool _isImportingAdif;

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
        if (_isImportingAdif)
            return;

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select ADIF file to import",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
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
            _isImportingAdif = true;
            var result = await Task.Run(() => AdifImportService.ImportFromFileAsync(path));
            RememberAdifDirectory(config, path);
            var message = result.ParsedCount == 0
                ? $"No QSO records were found in:\n{result.FilePath}"
                : $"Imported {result.ParsedCount} QSO record(s) from:\n{result.FilePath}\n\nDatabase change count: {result.SavedChanges}";

            await ShowMessageAsync(
                "ADIF Import Complete",
                message);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("ADIF Import Failed", ex.Message);
        }
        finally
        {
            _isImportingAdif = false;
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
            CanResize = false,
            Title = title,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        ok.Click += (_, _) => dialog.Close();

        var owner = GetPreferredDialogOwner();
        if (owner is not null)
            dialog.Show(owner);
        else
            dialog.Show();

        await Task.CompletedTask;
    }

    private Window? GetPreferredDialogOwner()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return IsVisible ? this : null;

        return desktop.Windows.FirstOrDefault(window => window.IsActive)
               ?? desktop.Windows.FirstOrDefault(window => window.IsVisible)
               ?? (IsVisible ? this : null);
    }

    private static void RememberAdifDirectory(AppConfiguration config, string importedFilePath)
    {
        var directory = Path.GetDirectoryName(importedFilePath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        var profile = AppConfigurationStore.GetActiveProfile(config);
        if (string.Equals(profile.AdifDirectory, directory, StringComparison.Ordinal))
            return;

        profile.AdifDirectory = directory;
        AppConfigurationStore.Save(config);
    }

    private async Task<Avalonia.Platform.Storage.IStorageFolder?> TryGetFolderFromPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return null;

        var fullPath = Path.GetFullPath(path);
        if (!Path.EndsInDirectorySeparator(fullPath))
            fullPath += Path.DirectorySeparatorChar;

        return await StorageProvider.TryGetFolderFromPathAsync(new Uri(fullPath));
    }
}




