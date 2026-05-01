namespace HamBusLog.Hardware;

using System.Collections.Concurrent;

public sealed class RigctldConnectionManager : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Worker> _workers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RadioRuntimeState> _states = new(StringComparer.OrdinalIgnoreCase);
    private int _reconnectIntervalSeconds = 3;

    public event EventHandler? StatesChanged;

    public Task<string> SetFrequencyByTagAsync(string tagName, decimal frequencyMhz, CancellationToken ct = default)
        => SetFrequencyByNameAsync(tagName, frequencyMhz, ct);

    public async Task<string> SetFrequencyByNameAsync(string radioName, decimal frequencyMhz, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(radioName))
            throw new ArgumentException("Radio name is required.", nameof(radioName));
        if (frequencyMhz <= 0)
            throw new ArgumentOutOfRangeException(nameof(frequencyMhz), "Frequency must be greater than zero.");

        var hz = (long)Math.Round(frequencyMhz * 1_000_000m);
        var command = new ControlCommand(ControlCommandType.SetFrequency, hz, null);
        await EnqueueControlCommandAsync(radioName.Trim(), command, ct);
        return $"Frequency set to {frequencyMhz:0.######} MHz";
    }

    public Task<string> SetModeByTagAsync(string tagName, string mode, CancellationToken ct = default)
        => SetModeByNameAsync(tagName, mode, ct);

    public async Task<string> SetModeByNameAsync(string radioName, string mode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(radioName))
            throw new ArgumentException("Radio name is required.", nameof(radioName));
        if (string.IsNullOrWhiteSpace(mode))
            throw new ArgumentException("Mode is required.", nameof(mode));

        var normalizedMode = mode.Trim().ToUpperInvariant();
        var command = new ControlCommand(ControlCommandType.SetMode, null, normalizedMode);
        await EnqueueControlCommandAsync(radioName.Trim(), command, ct);
        return $"Mode set to {normalizedMode}";
    }

    public async Task RefreshActiveConnectionsAsync()
    {
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);
        _reconnectIntervalSeconds = rigctld.ReconnectIntervalSeconds <= 0 ? 3 : Math.Min(rigctld.ReconnectIntervalSeconds, 300);

        // Poll every configured radio so status reflects all endpoints, even when a radio
        // is not currently selected as the active control target.
        var monitoredNames = rigctld.Radios
            .Select(x => x.RadioName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activeRadios = rigctld.Radios
            .Where(x => monitoredNames.Contains(x.RadioName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        List<string> toStop;
        lock (_gate)
        {
            toStop = _workers.Keys
                .Where(existingName => !monitoredNames.Contains(existingName, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var tag in toStop)
            await StopWorkerAsync(tag);

        foreach (var radio in activeRadios)
            StartWorkerIfNeeded(radio);
    }

    public IReadOnlyList<RadioRuntimeState> GetSnapshot()
    {
        lock (_gate)
        {
            return _states.Values
                .OrderBy(x => x.RadioName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public RadioRuntimeState? GetPrimaryActiveState()
    {
        lock (_gate)
        {
            return _states.Values
                .Where(x => x.IsConnected)
                .OrderByDescending(x => x.LastUpdatedUtc)
                .FirstOrDefault()
                ?? _states.Values.OrderByDescending(x => x.LastUpdatedUtc).FirstOrDefault();
        }
    }

    private void StartWorkerIfNeeded(RigRadioConfig radio)
    {
        var shouldNotify = false;
        lock (_gate)
        {
            if (_workers.ContainsKey(radio.RadioName))
                return;

            _states[radio.RadioName] = new RadioRuntimeState(
                radio.RadioName,
                radio.RadioName,
                false,
                null,
                0,
                null,
                $"Not connected ({radio.Host}:{radio.Port})",
                DateTime.UtcNow);

            var cts = new CancellationTokenSource();
            var worker = new Worker(radio, cts);
            worker.LoopTask = Task.Run(() => PollLoopAsync(worker, cts.Token), cts.Token);
            _workers[radio.RadioName] = worker;
            shouldNotify = true;
        }

        if (shouldNotify)
            OnStatesChanged();
    }

    private async Task StopWorkerAsync(string radioName)
    {
        Worker? worker;
        var shouldNotify = false;
        lock (_gate)
        {
            if (!_workers.TryGetValue(radioName, out worker))
                return;
            _workers.Remove(radioName);
        }

        try
        {
            worker.Cts.Cancel();
            await worker.LoopTask;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            while (worker.Commands.TryDequeue(out var pending))
                pending.Completion.TrySetException(new IOException("Radio service stopped before command execution."));

            worker.Cts.Dispose();
            lock (_gate)
            {
                shouldNotify = _states.Remove(radioName) || shouldNotify;
            }
        }

        if (shouldNotify)
            OnStatesChanged();
    }

    private async Task PollLoopAsync(Worker worker, CancellationToken ct)
    {
        var radio = worker.Radio;
        while (!ct.IsCancellationRequested)
        {
            using var client = new Wa1gonLib.RigControl.HamLibRigCtlClient(radio.Host, radio.Port);
            try
            {
                await client.OpenAsync();

                // Connected — mark online immediately even before first successful query.
                UpdateState(radio, true, null, 0, null, null);

                while (!ct.IsCancellationRequested)
                {
                    await ProcessControlCommandsAsync(worker, client, ct);

                    // Query mode and frequency independently.  A query error (e.g. rig
                    // backend returns RPRT -8 when no slice is open) does NOT disconnect —
                    // we stay connected and keep the last-known values for that field.
                    string? mode = null;
                    int passband = 0;
                    long? freqHz = null;
                    string? queryError = null;

                    try
                    {
                        (mode, passband) = await client.GetModeAndPassbandAsync();
                    }
                    catch (IOException ex) when (!IsConnectionLost(ex))
                    {
                        queryError = ex.Message;
                    }

                    try
                    {
                        freqHz = await client.GetFreqAsync();
                    }
                    catch (IOException ex) when (!IsConnectionLost(ex))
                    {
                        queryError ??= ex.Message;
                    }

                    UpdateState(radio, true, mode, passband, freqHz, queryError);
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // TCP-level failure — mark offline and wait before reconnecting.
                UpdateState(radio, false, null, 0, null, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(_reconnectIntervalSeconds), ct);
            }
        }
    }

    private async Task EnqueueControlCommandAsync(string radioName, ControlCommand command, CancellationToken ct)
    {
        Worker worker;
        RadioRuntimeState? state;
        lock (_gate)
        {
            if (!_workers.TryGetValue(radioName, out worker!))
                throw new InvalidOperationException($"No background service is running for radio '{radioName}'.");

            _states.TryGetValue(radioName, out state);
        }

        if (state is null || !state.IsConnected)
            throw new InvalidOperationException($"Radio '{radioName}' is not connected.");

        worker.Commands.Enqueue(command);
        try
        {
            await command.Completion.Task.WaitAsync(ct);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for radio '{radioName}' to accept the command.", ex);
        }
    }

    private static async Task ProcessControlCommandsAsync(Worker worker, Wa1gonLib.RigControl.HamLibRigCtlClient client, CancellationToken ct)
    {
        while (worker.Commands.TryDequeue(out var command))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                switch (command.Type)
                {
                    case ControlCommandType.SetFrequency when command.FrequencyHz is long hz:
                        await client.SetFreqAsync(hz);
                        command.Completion.TrySetResult();
                        break;
                    case ControlCommandType.SetMode when !string.IsNullOrWhiteSpace(command.Mode):
                        await client.SetModeAsync(command.Mode);
                        command.Completion.TrySetResult();
                        break;
                    default:
                        command.Completion.TrySetException(new InvalidOperationException("Invalid control command."));
                        break;
                }
            }
            catch (Exception ex)
            {
                command.Completion.TrySetException(ex);

                // Keep polling for rig-level errors (RPRT failures, unsupported ops, etc.).
                // Only bubble up when transport is actually lost so the worker reconnects.
                if (IsConnectionLost(ex))
                    throw;
            }
        }
    }

    private void UpdateState(RigRadioConfig radio, bool connected, string? mode, int passband, long? freqHz, string? error)
    {
        var shouldNotify = false;
        lock (_gate)
        {
            _states[radio.RadioName] = new RadioRuntimeState(
                radio.RadioName,
                radio.RadioName,
                connected,
                mode,
                passband,
                freqHz,
                error,
                DateTime.UtcNow);
            shouldNotify = true;
        }

        if (shouldNotify)
            OnStatesChanged();
    }

    /// <summary>
    /// Returns true when an exception signals a dropped TCP connection rather than
    /// a rig-level protocol error (e.g. RPRT -8 because a slice is closed).
    /// </summary>
    private static bool IsConnectionLost(Exception ex)
    {
        if (ex is not IOException ioEx)
            return false;

        var msg = ioEx.Message;
        return msg.Contains("Lost connection", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("connection reset", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Broken pipe", StringComparison.OrdinalIgnoreCase)
            || ioEx.InnerException is System.Net.Sockets.SocketException;
    }

    private void OnStatesChanged()
    {
        try
        {
            StatesChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        List<Task> tasks = [];
        lock (_gate)
        {
            foreach (var worker in _workers.Values)
            {
                worker.Cts.Cancel();
                tasks.Add(worker.LoopTask);
            }
            _workers.Clear();
            _states.Clear();
        }

        OnStatesChanged();

        try
        {
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(2));
        }
        catch
        {
        }
    }

    private sealed class Worker
    {
        public Worker(RigRadioConfig radio, CancellationTokenSource cts)
        {
            Radio = radio;
            Cts = cts;
        }

        public RigRadioConfig Radio { get; }
        public CancellationTokenSource Cts { get; }
        public ConcurrentQueue<ControlCommand> Commands { get; } = new();
        public Task LoopTask { get; set; } = Task.CompletedTask;
    }

    private enum ControlCommandType
    {
        SetFrequency,
        SetMode
    }

    private sealed class ControlCommand
    {
        public ControlCommand(ControlCommandType type, long? frequencyHz, string? mode)
        {
            Type = type;
            FrequencyHz = frequencyHz;
            Mode = mode;
        }

        public ControlCommandType Type { get; }
        public long? FrequencyHz { get; }
        public string? Mode { get; }
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

public sealed record RadioRuntimeState(
    string RadioName,
    string Label,
    bool IsConnected,
    string? Mode,
    int Passband,
    long? FrequencyHz,
    string? Error,
    DateTime LastUpdatedUtc)
{
    public decimal? FrequencyMhz => FrequencyHz is null ? null : Math.Round(FrequencyHz.Value / 1_000_000m, 3);

    /// <summary>Display string combining mode and passband, e.g. "USB 2400" or "-" when unknown.</summary>
    public string ModeDisplay
    {
        get
        {
            var hasMode = !string.IsNullOrWhiteSpace(Mode);
            var hasPassband = Passband > 0;
            if (!hasMode) return "-";
            return hasPassband ? $"{Mode} {Passband}" : Mode!;
        }
    }
}


