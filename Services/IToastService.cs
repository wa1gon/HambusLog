namespace HamBusLog.Services;

public interface IToastService
{
    void RegisterWindow(Window window);
    void ShowInfo(string title, string message, TimeSpan? timeout = null);
    void ShowSuccess(string title, string message, TimeSpan? timeout = null);
    void ShowWarning(string title, string message, TimeSpan? timeout = null);
    void ShowError(string title, string message, TimeSpan? timeout = null);
}

