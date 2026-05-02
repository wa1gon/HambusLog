using Avalonia.Controls.Notifications;

namespace HamBusLog.Services;

public sealed class ToastService : IToastService
{
    private INotificationManager? _notificationManager;

    public void RegisterWindow(Window window)
    {
        if (window is null)
            return;

        var manager = new WindowNotificationManager(window)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 4,
            Margin = new Thickness(12)
        };

        _notificationManager = manager;

        window.Activated += (_, _) => _notificationManager = manager;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_notificationManager, manager))
                _notificationManager = null;
        };
    }

    public void ShowInfo(string title, string message, TimeSpan? timeout = null)
        => Show(NotificationType.Information, title, message, timeout ?? TimeSpan.FromSeconds(30));

    public void ShowSuccess(string title, string message, TimeSpan? timeout = null)
        => Show(NotificationType.Success, title, message, timeout ?? TimeSpan.FromSeconds(30));

    public void ShowWarning(string title, string message, TimeSpan? timeout = null)
        => Show(NotificationType.Warning, title, message, timeout ?? TimeSpan.FromSeconds(30));

    public void ShowError(string title, string message, TimeSpan? timeout = null)
        => Show(NotificationType.Error, title, message, timeout ?? TimeSpan.FromSeconds(30));

    private void Show(NotificationType type, string title, string message, TimeSpan timeout)
    {
        var manager = _notificationManager;
        if (manager is null)
            return;

        manager.Show(new Notification(
            title?.Trim() ?? string.Empty,
            message?.Trim() ?? string.Empty,
            type,
            timeout));
    }
}

