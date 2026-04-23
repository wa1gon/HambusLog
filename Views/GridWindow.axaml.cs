namespace HamBusLog.Views;

public partial class GridWindow : Window
{
    private GridViewModel? _viewModel;

    public GridWindow()
    {
        InitializeComponent();
        _viewModel = new GridViewModel();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateUIForContestType();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GridViewModel.SelectedContestType))
        {
            UpdateUIForContestType();
        }
    }

    private void UpdateUIForContestType()
    {
        if (_viewModel == null) return;
        
        var modeLabel = this.FindControl<TextBlock>("ContestModeLabel");
        var fieldDayPanel = this.FindControl<StackPanel>("FieldDayExchange");
        var sentRstPanel = this.FindControl<StackPanel>("SentRstPanel");
        var recRstPanel = this.FindControl<StackPanel>("RecRstPanel");
        var isFieldDay = _viewModel.SelectedContestType == HamBusLog.ViewModels.ContestType.ArrlFieldDay;
        
        if (modeLabel != null)
        {
            modeLabel.Text = isFieldDay
                ? "Mode: ARRL Field Day"
                : "Mode: Normal QSO";
        }
        
        if (fieldDayPanel != null)
        {
            fieldDayPanel.IsVisible = isFieldDay;
        }

        if (sentRstPanel != null)
        {
            sentRstPanel.IsVisible = !isFieldDay;
        }

        if (recRstPanel != null)
        {
            recRstPanel.IsVisible = !isFieldDay;
        }
    }


    public void OnAddEntryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel?.AddNewEntry();
    }

    public void OnSortHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string column })
        {
            _viewModel?.SortBy(column);
        }
    }

    public void OnDateOrTimeFocus(object? sender, GotFocusEventArgs e)
    {
        var utcNow = DateTime.UtcNow;
        var dateValue = utcNow.ToString("yyyyMMdd");
        var timeValue = utcNow.ToString("HHmm");

        var dateInput = this.FindControl<TextBox>("DateInput");
        var timeInput = this.FindControl<TextBox>("TimeInput");

        if (dateInput != null)
            dateInput.Text = dateValue;
        if (timeInput != null)
            timeInput.Text = timeValue;

        if (_viewModel != null)
        {
            _viewModel.InputDate = dateValue;
            _viewModel.InputTimeOn = timeValue;
        }
    }

    public void OnCallInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var source = textBox.Text ?? string.Empty;
        var normalized = new string(source
            .ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '/')
            .ToArray());

        if (source != normalized)
        {
            var caret = textBox.CaretIndex;
            textBox.Text = normalized;
            textBox.CaretIndex = Math.Min(caret, normalized.Length);
        }

        if (_viewModel != null)
            _viewModel.InputCall = normalized;
    }

    public void OnSectionInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var source = textBox.Text ?? string.Empty;
        var normalized = source.ToUpperInvariant();

        if (source != normalized)
        {
            var caret = textBox.CaretIndex;
            textBox.Text = normalized;
            textBox.CaretIndex = Math.Min(caret, normalized.Length);
        }

        if (_viewModel != null)
            _viewModel.InputFieldDaySection = normalized;
    }

    public void OnClassInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var source = textBox.Text ?? string.Empty;
        var normalized = source.ToUpperInvariant();

        if (source != normalized)
        {
            var caret = textBox.CaretIndex;
            textBox.Text = normalized;
            textBox.CaretIndex = Math.Min(caret, normalized.Length);
        }

        if (_viewModel != null)
            _viewModel.InputFieldDayClass = normalized;
    }

    public void OnBandInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var source = textBox.Text ?? string.Empty;
        var normalized = source.ToUpperInvariant();

        if (source != normalized)
        {
            var caret = textBox.CaretIndex;
            textBox.Text = normalized;
            textBox.CaretIndex = Math.Min(caret, normalized.Length);
        }

        if (_viewModel != null)
            _viewModel.InputBand = normalized;
    }

    public void OnModeInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var source = textBox.Text ?? string.Empty;
        var normalized = source.ToUpperInvariant();

        if (source != normalized)
        {
            var caret = textBox.CaretIndex;
            textBox.Text = normalized;
            textBox.CaretIndex = Math.Min(caret, normalized.Length);
        }

        if (_viewModel != null)
            _viewModel.InputMode = normalized;
    }
}
