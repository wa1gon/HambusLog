using Avalonia.Controls;
using Avalonia.Input;
using HamBusLog.ViewModels;
using Avalonia.VisualTree;

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
                OpenOrActivateGridWindow();
                
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

    private void OpenOrActivateGridWindow()
    {
        if (_gridWindow is { IsVisible: true })
        {
            _gridWindow.Activate();
            return;
        }

        _gridWindow = new GridWindow();
        _gridWindow.Closed += (_, _) => _gridWindow = null;
        _gridWindow.Show();
    }
}