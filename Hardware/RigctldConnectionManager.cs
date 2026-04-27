namespace HamBusLog.Hardware;

public sealed class RigctldConnectionManager : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Worker> _workers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RadioRuntimeState> _states = new(StringComparer.OrdinalIgnoreCase);

    public async Task RefreshActiveConnectionsAsync()
    {
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);

        var activeTags = rigctld.ActiveRadioTags
            .Concat(rigctld.Radios.Where(x => x.IsActive).Select(x => x.TagName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activeRadios = rigctld.Radios
            .Where(x => activeTags.Contains(x.TagName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        List<string> toStop;
        lock (_gate)
        {
            toStop = _workers.Keys
                .Where(existingTag => !activeTags.Contains(existingTag, StringComparer.OrdinalIgnoreCase))
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
                .OrderBy(x => x.TagName, StringComparer.OrdinalIgnoreCase)
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
        lock (_gate)
        {
            if (_workers.ContainsKey(radio.TagName))
                return;

            var cts = new CancellationTokenSource();
            var worker = new Worker(radio, cts, Task.Run(() => PollLoopAsync(radio, cts.Token), cts.Token));
            _workers[radio.TagName] = worker;
        }
    }

    private async Task StopWorkerAsync(string tagName)
    {
        Worker? worker;
        lock (_gate)
        {
            if (!_workers.TryGetValue(tagName, out worker))
                return;
            _workers.Remove(tagName);
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
            worker.Cts.Dispose();
            lock (_gate)
            {
                _states.Remove(tagName);
            }
        }
    }

    private async Task PollLoopAsync(RigRadioConfig radio, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var client = new Wa1gonLib.RigControl.HamLibRigCtlClient(radio.Host, radio.Port);
            try
            {
                await client.OpenAsync();
                while (!ct.IsCancellationRequested)
                {
                    var mode = await client.GetModeAsync();
                    var freqHz = await client.GetFreqAsync();
                    UpdateState(radio, true, mode, freqHz, null);
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                UpdateState(radio, false, null, null, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    private void UpdateState(RigRadioConfig radio, bool connected, string? mode, long? freqHz, string? error)
    {
        lock (_gate)
        {
            _states[radio.TagName] = new RadioRuntimeState(
                radio.TagName,
                string.IsNullOrWhiteSpace(radio.DisplayName) ? radio.TagName : radio.DisplayName,
                connected,
                mode,
                freqHz,
                error,
                DateTime.UtcNow);
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

        try
        {
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(2));
        }
        catch
        {
        }
    }

    private sealed record Worker(RigRadioConfig Radio, CancellationTokenSource Cts, Task LoopTask);
}

public sealed record RadioRuntimeState(
    string TagName,
    string Label,
    bool IsConnected,
    string? Mode,
    long? FrequencyHz,
    string? Error,
    DateTime LastUpdatedUtc)
{
    public decimal? FrequencyMhz => FrequencyHz is null ? null : Math.Round(FrequencyHz.Value / 1_000_000m, 6);
}

