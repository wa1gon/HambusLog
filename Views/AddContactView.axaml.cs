namespace HamBusLog.Views;

using Avalonia.Controls;
using HamBusLog.Data;
using HamBusLog.Services;

public partial class AddContactView : UserControl
{
    public AddContactView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        ApplyStayOnTopSetting();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        ApplyStayOnTopSetting();
    }

    public void OnUppercaseInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var current = textBox.Text ?? string.Empty;
        var upper = current.ToUpperInvariant();
        if (string.Equals(current, upper, StringComparison.Ordinal))
            return;

        var caret = textBox.CaretIndex;
        textBox.Text = upper;
        textBox.CaretIndex = Math.Min(caret, upper.Length);
    }

    private void ApplyStayOnTopSetting()
    {
        if (VisualRoot is not Window hostWindow)
            return;

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        hostWindow.Topmost = profile.StayOnTopAddContactWindow;

        var checkBox = this.FindControl<CheckBox>("StayOnTopCheckBox");
        if (checkBox is not null)
            checkBox.IsChecked = hostWindow.Topmost;
    }

    private void SaveStayOnTopSetting(bool isEnabled)
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        profile.StayOnTopAddContactWindow = isEnabled;
        AppConfigurationStore.Save(config);
    }

    private void UpdateStayOnTop(bool isEnabled)
    {
        if (VisualRoot is not Window hostWindow)
            return;

        hostWindow.Topmost = isEnabled;
        SaveStayOnTopSetting(isEnabled);
    }

    private void OnStayOnTopChecked(object? sender, RoutedEventArgs e)
    {
        UpdateStayOnTop(true);
    }

    private void OnStayOnTopUnchecked(object? sender, RoutedEventArgs e)
    {
        UpdateStayOnTop(false);
    }

    public async void OnLookupClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AddContactViewModel viewModel)
            return;

        var config = AppConfigurationStore.Load();
        var service = CallsignLookupService.CreateDefault(config);
        var (result, errorMessage) = await service.LookupAsync(viewModel.InputCall, CancellationToken.None);
        if (result is null)
        {
            var message = string.IsNullOrWhiteSpace(errorMessage) ? "Lookup failed." : errorMessage;
            App.Toasts.ShowWarning("Callsign lookup", message);
            return;
        }

        viewModel.ApplyLookupResult(result);
        App.Toasts.ShowSuccess("Callsign lookup", $"Found {result.CallSign} via {result.Provider}.");
    }
}
