using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.Views;

public partial class QsoEditWindow
{
    private readonly QsoEditViewModel _viewModel;
    private Guid _qsoId;

    public event EventHandler<Qso>? QsoSaved;

    public QsoEditWindow()
    {
        InitializeComponent();
        _viewModel = new QsoEditViewModel();
        DataContext = _viewModel;
    }

    public QsoEditWindow(Qso source)
        : this()
    {
        _qsoId = source.Id;
        _viewModel.LoadFrom(source);
    }

    public void OnAddDetailClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.AddDetail();
    }

    public void OnRemoveDetailClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.RemoveSelectedDetail();
    }

    public void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var updated = _viewModel.BuildUpdatedQso(_qsoId);
            QsoSaved?.Invoke(this, updated);
            Close();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    public void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetStatus(string message)
    {
        var label = this.FindControl<TextBlock>("StatusLabel");
        if (label != null)
            label.Text = message;
    }
}


