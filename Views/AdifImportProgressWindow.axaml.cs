namespace HamBusLog.Views;

public partial class AdifImportProgressWindow : Window
{
    private readonly AdifImportProgressViewModel _viewModel;

    public AdifImportProgressWindow()
    {
        InitializeComponent();
        _viewModel = new AdifImportProgressViewModel();
        DataContext = _viewModel;
    }

    public void UpdateProgress(AdifImportProgress progress)
    {
        _viewModel.Update(progress);
        Title = _viewModel.WindowTitle;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_viewModel.IsCompleted)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }
}

