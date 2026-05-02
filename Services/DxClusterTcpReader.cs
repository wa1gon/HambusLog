namespace HamBusLog.Services;

using System.Net.Sockets;
using System.Text;

public sealed class DxClusterTcpReader : IDxClusterTcpReader
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public bool IsRunning => _runTask is { IsCompleted: false };

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            if (IsRunning)
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

        if (toAwait is null)
            return;

        try
        {
            await toAwait.WaitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Normal when stopping.
        }
        catch
        {
            // Reader loop handles retrying/logging; swallow during shutdown.
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

    private static async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var config = AppConfigurationStore.Load();
                var cluster = config.Cluster ?? new ClusterConfig();
                var host = string.IsNullOrWhiteSpace(cluster.Hostname) ? "127.0.0.1" : cluster.Hostname.Trim();
                var port = cluster.TcpPort <= 0 ? 7300 : cluster.TcpPort;

                using var client = new TcpClient();
                await client.ConnectAsync(host, port, ct);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true };

                var callsign = cluster.Callsign?.Trim() ?? string.Empty;
                var password = cluster.Password ?? string.Empty;
                var command = cluster.Command?.Trim() ?? string.Empty;

                var loginSent = false;
                var passwordSent = false;
                var commandSent = false;

                if (!string.IsNullOrWhiteSpace(callsign))
                {
                    await writer.WriteLineAsync(callsign);
                    loginSent = true;
                }

                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line is null)
                        break;

                    var lowered = line.Trim().ToLowerInvariant();

                    if (!loginSent && !string.IsNullOrWhiteSpace(callsign)
                        && (lowered.Contains("login") || lowered.Contains("call:")))
                    {
                        await writer.WriteLineAsync(callsign);
                        loginSent = true;
                        continue;
                    }

                    if (!passwordSent && !string.IsNullOrWhiteSpace(password) && lowered.Contains("password"))
                    {
                        await writer.WriteLineAsync(password);
                        passwordSent = true;
                        continue;
                    }

                    if (!commandSent && !string.IsNullOrWhiteSpace(command) && loginSent)
                    {
                        await writer.WriteLineAsync(command);
                        commandSent = true;
                    }

                    _ = App.DxSpotFeed.PublishLine(line);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DX cluster reader error: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore disposal errors.
        }

        _lifecycleGate.Dispose();
    }
}

