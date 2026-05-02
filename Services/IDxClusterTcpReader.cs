namespace HamBusLog.Services;

public interface IDxClusterTcpReader : IDisposable
{
    bool IsRunning { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task RestartAsync(CancellationToken ct = default);
}

