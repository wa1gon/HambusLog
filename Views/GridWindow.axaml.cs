using Avalonia.Controls;
using HamBusLog.ViewModels;

namespace HamBusLog.Views;

public partial class GridWindow : Window
{
    public GridWindow()
    {
        InitializeComponent();
        DataContext = new GridViewModel();
    }
}

