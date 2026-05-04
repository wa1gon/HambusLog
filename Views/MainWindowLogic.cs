namespace HamBusLog.Views;

public partial class MainWindow
{
    private MenuNode? _previousSelection;
    private GridWindow? _gridWindow;
    private DxSpotsWindow? _dxSpotsWindow;
    private bool _isImportingAdif;

    public MainWindow()
    {
        InitializeComponent();
        App.TrackWindowPlacement(this, nameof(MainWindow));
        App.Toasts.RegisterWindow(this);
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
            else if (node.Title == "Import JADE")
            {
                await ImportJadeAsync();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Export ADIF")
            {
                await ExportAdifAsync();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Export JADE")
            {
                await ExportJadeAsync();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Export JADE Schema")
            {
                await ExportJadeSchemaAsync();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Export JADE Example")
            {
                await ExportJadeExampleAsync();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "DX Spots" || node.Title == "DX Cluster")
            {
                ToggleDxSpotsWindow();
                ResetTreeSelection(sender);
            }
            else
            {
                _previousSelection = node;
            }
        }
    }

    public void OnOpenGridClicked(object? sender, RoutedEventArgs e) => ToggleGridWindow();

    public void OnOpenNewContactClicked(object? sender, RoutedEventArgs e) => OpenNewContactWindow();

    public void OnOpenConfigurationClicked(object? sender, RoutedEventArgs e) => OpenConfigurationWindow();

    public async void OnImportAdifClicked(object? sender, RoutedEventArgs e) => await ImportAdifAsync();

    public void OnOpenDxClusterClicked(object? sender, RoutedEventArgs e) => ToggleDxSpotsWindow();

    private void ToggleGridWindow()
    {
        if (_gridWindow is { IsVisible: true })
        {
            App.SaveWindowPlacement(_gridWindow, nameof(GridWindow));
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

    private void ToggleDxSpotsWindow()
    {
        if (_dxSpotsWindow is { IsVisible: true })
        {
            App.SaveWindowPlacement(_dxSpotsWindow, nameof(DxSpotsWindow));
            _dxSpotsWindow.Hide();
            return;
        }

        if (_dxSpotsWindow is null)
        {
            _dxSpotsWindow = new DxSpotsWindow();
            _dxSpotsWindow.Closed += (_, _) => _dxSpotsWindow = null;
        }

        ShowWithVisibleOwner(_dxSpotsWindow);
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
        var progressWindow = new AdifImportProgressWindow();
        var owner = GetPreferredDialogOwner();
        var progress = new Progress<AdifImportProgress>(update => progressWindow.UpdateProgress(update));
        AdifImportResult? result = null;
        Exception? importException = null;

        try
        {
            _isImportingAdif = true;
            progressWindow.UpdateProgress(AdifImportProgress.Starting(path));
            if (owner is not null)
                progressWindow.Show(owner);
            else
                progressWindow.Show();

            result = await Task.Run(() => AdifImportService.ImportFromFileAsync(path, progress: progress));
            RememberAdifDirectory(config, path);
        }
        catch (Exception ex)
        {
            importException = ex;
        }
        finally
        {
            if (result is null)
                progressWindow.UpdateProgress(AdifImportProgress.Completed(path, 0, 0));

            progressWindow.Close();
            _isImportingAdif = false;
        }

        if (importException is not null)
        {
            App.Toasts.ShowError("ADIF import failed", importException.Message);
            return;
        }

        if (result is not null)
        {
            if (result.Value.ParsedCount == 0)
            {
                App.Toasts.ShowWarning("ADIF import complete", "No QSO records were found in the selected file.");
                return;
            }

            App.Toasts.ShowSuccess(
                "ADIF import complete",
                $"Imported {result.Value.ParsedCount} QSO record(s). Database changes: {result.Value.SavedChanges}."
                + (result.Value.DuplicateCount > 0 ? $" Skipped duplicates: {result.Value.DuplicateCount}." : string.Empty));
        }
    }

    private async Task ImportJadeAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select JADE file to import",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeFilter =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] },
                new Avalonia.Platform.Storage.FilePickerFileType("All files") { Patterns = ["*"] }
            ]
        });

        if (files.Count == 0)
            return;

        var path = files[0].Path.LocalPath;
        try
        {
            var imported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(path);
            if (imported == 0)
            {
                App.Toasts.ShowWarning("JADE import complete", "No QSO records were found in the selected file.");
                return;
            }

            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE import complete", $"Imported {imported} QSO record(s).");
        }
        catch (HamBusLog.Wa1gonLib.Exchange.JadeValidationException ex)
        {
            var topErrors = ex.Errors.Take(4).ToList();
            var moreCount = Math.Max(0, ex.Errors.Count - topErrors.Count);
            var details = string.Join("\n", topErrors);
            if (moreCount > 0)
                details += $"\n...and {moreCount} more issue(s).";

            App.Toasts.ShowError("JADE import validation failed", details);
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE import failed", ex.Message);
        }
    }

    private async Task ExportAdifAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export ADIF",
            SuggestedFileName = $"hambuslog-{DateTime.UtcNow:yyyyMMdd-HHmmss}.adi",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("ADIF files") { Patterns = ["*.adi", "*.adif"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        try
        {
            var exported = await AdifExportService.ExportToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("ADIF export complete", $"Exported {exported} QSO record(s).");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("ADIF export failed", ex.Message);
        }
    }

    private async Task ExportJadeAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export JADE",
            SuggestedFileName = $"hambuslog-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        try
        {
            var exported = await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE export complete", $"Exported {exported} QSO record(s).");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE export failed", ex.Message);
        }
    }

    private async Task ExportJadeSchemaAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export JADE Schema Only",
            SuggestedFileName = "jade-schema-template.json",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        try
        {
            await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportSchemaToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE schema export complete", "Exported JADE schema/template with empty records array.");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE schema export failed", ex.Message);
        }
    }

    private async Task ExportJadeExampleAsync()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var suggestedStartLocation = await TryGetFolderFromPathAsync(profile.AdifDirectory);
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export JADE Example Record",
            SuggestedFileName = "jade-example-record.json",
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("JADE JSON files") { Patterns = ["*.json", "*.jade"] }
            ]
        });

        if (file is null)
            return;

        var path = file.Path.LocalPath;
        try
        {
            await HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportExampleToFileAsync(path);
            RememberAdifDirectory(config, path);
            App.Toasts.ShowSuccess("JADE example export complete", "Exported JADE template with one populated example record.");
        }
        catch (Exception ex)
        {
            App.Toasts.ShowError("JADE example export failed", ex.Message);
        }
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

    private async void OnApplyRadioFrequencyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.ApplyFrequencyToSelectedRadioAsync();
    }

    private async void OnApplyRadioModeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        await vm.ApplyModeToSelectedRadioAsync();
    }

    private async void OnApplyPresetModeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (sender is not Button { Tag: string mode } || string.IsNullOrWhiteSpace(mode))
            return;

        await vm.ApplyPresetModeToSelectedRadioAsync(mode);
    }

    private void OnRadioRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (sender is Border { DataContext: RadioConnectionStatusViewModel row })
            vm.SelectedRadioStatus = row;
    }
}

