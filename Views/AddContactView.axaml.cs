namespace HamBusLog.Views;

using Avalonia.Controls;

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
}
