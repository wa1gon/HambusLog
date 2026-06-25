namespace HamBusLog.Services;

public interface IWsjtBridgeService : IDisposable
{
    bool IsRunning { get; }

    event EventHandler<WsjtTrafficEvent>? TrafficObserved;
    event EventHandler<WsjtLoggedQso>? LoggedQsoReceived;

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task RestartAsync(CancellationToken ct = default);

    IReadOnlyList<WsjtTrafficEvent> GetTrafficSnapshot();
}

