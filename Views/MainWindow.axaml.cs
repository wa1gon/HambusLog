using Avalonia.Controls;
using Avalonia.Input;
using HamBusLog.ViewModels;
using Avalonia.VisualTree;

namespace HamBusLog.Views;

public partial class MainWindow : Window
{
    private MenuNode? _previousSelection;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void OnMenuTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is MenuNode node)
        {
            if (node.Title == "Grid")
            {
                var gridWindow = new GridWindow();
                gridWindow.Show();
                
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
}