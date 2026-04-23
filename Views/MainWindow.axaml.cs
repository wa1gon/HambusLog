using Avalonia.Controls;
using HamBusLog.ViewModels;

namespace HamBusLog.Views;

public partial class MainWindow : Window
{
    private MenuNode? _previousSelection;
    private GridWindow? _gridWindow;

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
                
                // Reset selection to previous item
                if (_previousSelection != null)
                {
                    if (sender is TreeView treeView)
                    {
                        treeView.SelectedItem = _previousSelection;
                    }
                }
            }
            else if (node.Title == "Settings")
            {
                OpenSettingsWindow();
                
                // Reset selection to previous item
                if (_previousSelection != null)
                {
                    if (sender is TreeView treeView)
                    {
                        treeView.SelectedItem = _previousSelection;
                    }
                }
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
        settingsWindow.ShowDialog(this);
    }
}