namespace HamBusLog.Services;

using System.Net;
using System.Net.Sockets;

public sealed class WsjtBridgeService : IWsjtBridgeService
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly object _trafficSync = new();
    private readonly WsjtMessageParser _parser = new();
    private readonly List<WsjtTrafficEvent> _traffic = [];

    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public bool IsRunning => _runTask is { IsCompleted: false };

    public event EventHandler<WsjtTrafficEvent>? TrafficObserved;
    public event EventHandler<WsjtLoggedQso>? LoggedQsoReceived;

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            if (IsRunning)
                return;

            var config = AppConfigurationStore.Load();
            if (!config.Wsjt.Enabled)
                return;

            _cts = new CancellationTokenSource();
            _runTask = RunLoopAsync(_cts.Token);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        Task? toAwait;

        await _lifecycleGate.WaitAsync(ct);
        try
        {
            if (_cts is null)
                return;

            _cts.Cancel();
            toAwait = _runTask;
        }
        finally
        {
            _lifecycleGate.Release();
        }

        if (toAwait is not null)
        {
            try
            {
                await toAwait.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch
            {
                // Loop handles recoverable errors.
            }
        }

        await _lifecycleGate.WaitAsync(ct);
        try
        {
            _cts?.Dispose();
            _cts = null;
            _runTask = null;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task RestartAsync(CancellationToken ct = default)
    {
        await StopAsync(ct);
        await StartAsync(ct);
    }

    public IReadOnlyList<WsjtTrafficEvent> GetTrafficSnapshot()
    {
        lock (_trafficSync)
            return _traffic.ToList();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpClient? udp = null;
            try
            {
                var config = AppConfigurationStore.Load();
                var wsjt = config.Wsjt;
                if (!wsjt.Enabled)
                    return;

                var listenAddress = ParseAddress(wsjt.ListenAddress);
                var listenPort = NormalizePort(wsjt.ListenPort);

                udp = new UdpClient(new IPEndPoint(listenAddress, listenPort));
                PublishTraffic("SYS", WsjtMessageType.Status, "hambuslog", "WSJT listener ready", $"Listening on {listenAddress}:{listenPort}", []);

                while (!ct.IsCancellationRequested)
                {
                    var result = await udp.ReceiveAsync(ct);
                    var payload = result.Buffer;
                    var remote = result.RemoteEndPoint;
                    var remoteAddress = remote.Address;

                    if (wsjt.AcceptOnlyLocalhost && !IPAddress.IsLoopback(remoteAddress))
                    {
                        PublishTraffic("RX", WsjtMessageType.Unknown, remote.ToString(), "Dropped non-local packet", string.Empty, payload);
                        continue;
                    }

                    if (!_parser.TryParse(payload, out var parsed))
                    {
                        PublishTraffic("RX", WsjtMessageType.Unknown, remote.ToString(), "Unrecognized WSJT-X packet", string.Empty, payload);
                        continue;
                    }

                    PublishTraffic("RX", parsed.MessageType, parsed.ClientId, parsed.Summary, parsed.DecodedText, payload);

                    if (parsed.MessageType != WsjtMessageType.LoggedAdif || string.IsNullOrWhiteSpace(parsed.LoggedAdif))
                        continue;

                    if (_parser.TryBuildLoggedQso(parsed.LoggedAdif, out var qso))
                        LoggedQsoReceived?.Invoke(this, qso);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                PublishTraffic("SYS", WsjtMessageType.Unknown, "hambuslog", "WSJT listener failed to bind", "Address already in use; another process is already listening on this UDP port.", []);
                break;
            }
            catch (Exception ex)
            {
                PublishTraffic("SYS", WsjtMessageType.Unknown, "hambuslog", "WSJT listener error: " + ex.Message, string.Empty, []);
            }
            finally
            {
                try
                {
                    udp?.Dispose();
                }
                catch
                {
                    // ignore during shutdown
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void PublishTraffic(string direction, WsjtMessageType messageType, string clientId, string summary, string decodedText, byte[] payload)
    {
        var entry = new WsjtTrafficEvent(
            DateTimeOffset.UtcNow,
            direction,
            messageType,
            clientId,
            summary,
            decodedText,
            payload);

        lock (_trafficSync)
        {
            _traffic.Add(entry);
            var max = GetConfiguredQueueLength();
            while (_traffic.Count > max)
                _traffic.RemoveAt(0);
        }

        TrafficObserved?.Invoke(this, entry);
    }

    private static int GetConfiguredQueueLength()
    {
        var config = AppConfigurationStore.Load();
        return config.Wsjt.DebugQueueLength <= 0 ? 500 : config.Wsjt.DebugQueueLength;
    }

    private static IPAddress ParseAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return IPAddress.Any;

        return IPAddress.TryParse(value.Trim(), out var parsed)
            ? parsed
            : IPAddress.Any;
    }

    private static int NormalizePort(int value)
        => value <= 0 ? 2237 : value;

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore disposal failures.
        }

        _lifecycleGate.Dispose();
    }
}







