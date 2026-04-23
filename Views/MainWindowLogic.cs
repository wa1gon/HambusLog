namespace HamBusLog.Views;

public partial class MainWindow
{
    private MenuNode? _previousSelection;
    private GridWindow? _gridWindow;
    private RigCatalogWindow? _rigCatalogWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void OnMenuTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuNode node)
        {
            if (node.Title == "Grid" || node.Title == "Open/Reopen Grid")
            {
                ToggleGridWindow();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Settings")
            {
                OpenSettingsWindow();
                ResetTreeSelection(sender);
            }
            else if (node.Title == "Rig Catalog")
            {
                OpenRigCatalogWindow();
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

        _gridWindow.Show();
    }

    private void OpenSettingsWindow()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Show();
    }

    private void OpenRigCatalogWindow()
    {
        if (_rigCatalogWindow is { IsVisible: true })
        {
            return;
        }
        _rigCatalogWindow = new RigCatalogWindow();
        _rigCatalogWindow.Closed += (_, _) => _rigCatalogWindow = null;
        _rigCatalogWindow.Show();
    }

    private void ResetTreeSelection(object? sender)
    {
        if (_previousSelection != null && sender is TreeView tv)
            tv.SelectedItem = _previousSelection;
    }
}


