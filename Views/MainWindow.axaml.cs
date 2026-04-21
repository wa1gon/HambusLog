using Avalonia.Controls;
using Avalonia.Input;
using HamBusLog.ViewModels;
using Avalonia.VisualTree;

namespace HamBusLog.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void OnTreeViewItemPressed(object? sender, PointerPressedEventArgs e)
    {
        // Placeholder - will be replaced with OnTreeViewPointerPressed
    }

    public void OnTreeViewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var treeViewItem = e.Source as TreeViewItem ?? (e.Source as Control)?.FindAncestorOfType<TreeViewItem>();
        if (treeViewItem != null && treeViewItem.DataContext is MenuNode node && node.HasChildren)
        {
            treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
        }
    }

    public void OnTreeViewItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is MenuNode node && node.HasChildren)
        {
            treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
            e.Handled = true;
        }
    }
}