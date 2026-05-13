using Avalonia.Controls;

namespace HamBusLog.Views;

public partial class OperationProgressWindow : Window
{
    public OperationProgressWindow()
    {
        InitializeComponent();
    }

    public OperationProgressWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    public void UpdateMessage(string message)
    {
        MessageText.Text = message;
    }
}
