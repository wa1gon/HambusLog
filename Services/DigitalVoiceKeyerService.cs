namespace HamBusLog.Services;

public sealed class DigitalVoiceKeyerService : IDigitalVoiceKeyerService
{
    private const int SlotsPerBank = 10;
    private const string RecordingFolderName = "dvk";
    public const string SystemDefaultPlaybackDevice = "System Default";

    private readonly object _sync = new();
    private string? _activeRecordingKey;
    private Process? _activeRecordingProcess;

    public event EventHandler? BankChanged;

    public IReadOnlyList<string> GetAvailablePlaybackDevices()
    {
        var devices = new List<string> { SystemDefaultPlaybackDevice };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SystemDefaultPlaybackDevice };

        foreach (var device in EnumeratePactlSinks())
        {
            if (seen.Add(device))
                devices.Add(device);
        }

        foreach (var device in EnumerateAlsaPlaybackDevices())
        {
            if (seen.Add(device))
                devices.Add(device);
        }

        return devices;
    }

    public string GetPreferredPlaybackDevice()
    {
        var config = AppConfigurationStore.Load();
        var normalized = NormalizePlaybackDevice(config.DigitalVoiceKeyer.OutputDevice);
        return string.IsNullOrWhiteSpace(normalized) ? SystemDefaultPlaybackDevice : normalized;
    }

    public void SetPreferredPlaybackDevice(string? deviceName)
    {
        var normalized = NormalizePlaybackDevice(deviceName);

        lock (_sync)
        {
            var config = AppConfigurationStore.Load();
            config.DigitalVoiceKeyer.OutputDevice = normalized;
            AppConfigurationStore.Save(config);
        }
    }

    public IReadOnlyList<DigitalVoiceKeyerRecord> GetRecordsForLogType(string? logTypeKey)
    {
        var normalizedLogTypeKey = NormalizeLogTypeKey(logTypeKey);
        var config = AppConfigurationStore.Load();
        var bankWasCreated = false;
        var bank = GetOrCreateBank(config, normalizedLogTypeKey, ref bankWasCreated);
        bank.Records = NormalizeConfigRecords(normalizedLogTypeKey, bank.Records);

        if (bankWasCreated)
            AppConfigurationStore.Save(config);

        return bank.Records
            .OrderBy(x => x.SlotNumber)
            .Select(x => new DigitalVoiceKeyerRecord
            {
                SlotNumber = x.SlotNumber,
                Label = x.Label,
                Message = x.Message,
                RecordingPath = x.RecordingPath,
                HasRecording = !string.IsNullOrWhiteSpace(x.RecordingPath) && File.Exists(x.RecordingPath),
                IsRecording = string.Equals(_activeRecordingKey, MakeSessionKey(normalizedLogTypeKey, x.SlotNumber), StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    public void SaveRecordsForLogType(string? logTypeKey, IEnumerable<DigitalVoiceKeyerRecord> records)
    {
        var normalizedLogTypeKey = NormalizeLogTypeKey(logTypeKey);
        lock (_sync)
        {
            var config = AppConfigurationStore.Load();
            var bankWasCreated = false;
            var bank = GetOrCreateBank(config, normalizedLogTypeKey, ref bankWasCreated);

            bank.Records = NormalizeInputRecords(normalizedLogTypeKey, records);
            AppConfigurationStore.Save(config);
        }

        RaiseBankChanged();
    }

    public async Task<DigitalVoiceKeyerOperationResult> RecordSlotAsync(string? logTypeKey, int slotNumber, CancellationToken cancellationToken = default)
    {
        var normalizedLogTypeKey = NormalizeLogTypeKey(logTypeKey);
        slotNumber = NormalizeSlot(slotNumber);
        var sessionKey = MakeSessionKey(normalizedLogTypeKey, slotNumber);

        lock (_sync)
        {
            if (_activeRecordingProcess is not null && !_activeRecordingProcess.HasExited)
            {
                if (!string.Equals(_activeRecordingKey, sessionKey, StringComparison.OrdinalIgnoreCase))
                    return DigitalVoiceKeyerOperationResult.Fail("Stop the active recording before starting another slot.");

                return StopRecordingLocked(normalizedLogTypeKey, slotNumber);
            }

            return StartRecordingLocked(normalizedLogTypeKey, slotNumber, cancellationToken);
        }
    }

    public async Task<DigitalVoiceKeyerOperationResult> PlaySlotAsync(string? logTypeKey, int slotNumber, CancellationToken cancellationToken = default)
    {
        var normalizedLogTypeKey = NormalizeLogTypeKey(logTypeKey);
        slotNumber = NormalizeSlot(slotNumber);
        var slotPath = GetRecordingPath(normalizedLogTypeKey, slotNumber);

        if (!File.Exists(slotPath))
            return DigitalVoiceKeyerOperationResult.Fail($"No recording found for slot {slotNumber}.");

        try
        {
            var preferredDevice = GetConfiguredPlaybackDevice();
            ProcessRunResult? lastFailure = null;
            foreach (var attempt in BuildPlaybackAttempts(slotPath, preferredDevice))
            {
                var result = await RunPlaybackAsync(attempt.FileName, attempt.Arguments, cancellationToken);
                if (result.ExitCode == 0)
                    return DigitalVoiceKeyerOperationResult.Ok($"Played slot {slotNumber}.");

                lastFailure = result;
            }

            var errorMessage = lastFailure is null
                ? "Playback failed."
                : string.IsNullOrWhiteSpace(lastFailure.Value.StandardError)
                    ? lastFailure.Value.StandardOutput.Trim()
                    : lastFailure.Value.StandardError.Trim();
            return DigitalVoiceKeyerOperationResult.Fail(string.IsNullOrWhiteSpace(errorMessage) ? "Playback failed." : errorMessage);
        }
        catch (OperationCanceledException)
        {
            return DigitalVoiceKeyerOperationResult.Fail("Playback canceled.");
        }
        catch (Exception ex)
        {
            return DigitalVoiceKeyerOperationResult.Fail(ex.Message);
        }
    }

    public bool DeleteRecording(string? logTypeKey, int slotNumber)
    {
        var normalizedLogTypeKey = NormalizeLogTypeKey(logTypeKey);
        slotNumber = NormalizeSlot(slotNumber);
        var sessionKey = MakeSessionKey(normalizedLogTypeKey, slotNumber);
        var slotPath = GetRecordingPath(normalizedLogTypeKey, slotNumber);

        lock (_sync)
        {
            if (string.Equals(_activeRecordingKey, sessionKey, StringComparison.OrdinalIgnoreCase))
                StopRecordingProcessLocked();

            try
            {
                if (File.Exists(slotPath))
                    File.Delete(slotPath);

                var config = AppConfigurationStore.Load();
                var bankWasCreated = false;
                var bank = GetOrCreateBank(config, normalizedLogTypeKey, ref bankWasCreated);
                var record = bank.Records.First(x => x.SlotNumber == slotNumber);
                record.RecordingPath = string.Empty;
                record.IsRecording = false;
                AppConfigurationStore.Save(config);
            }
            catch
            {
                return false;
            }
        }

        RaiseBankChanged();
        return true;
    }

    private DigitalVoiceKeyerOperationResult StartRecordingLocked(string logTypeKey, int slotNumber, CancellationToken cancellationToken)
    {
        var slotPath = GetRecordingPath(logTypeKey, slotNumber);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(slotPath) ?? AppContext.BaseDirectory);

            var started = TryStartRecordingProcess("pw-record", $"--channels 1 --rate 48000 --format s16 \"{slotPath}\"", out var process)
                         || TryStartRecordingProcess("arecord", $"-q -f S16_LE -c 1 -r 48000 -t wav \"{slotPath}\"", out process);

            if (!started || process is null)
                return DigitalVoiceKeyerOperationResult.Fail("Could not start audio recording. Verify pw-record or arecord is installed and a microphone is available.");

            _activeRecordingKey = MakeSessionKey(logTypeKey, slotNumber);
            _activeRecordingProcess = process;

            UpdateRecordingState(logTypeKey, slotNumber, true, slotPath);
            return DigitalVoiceKeyerOperationResult.Ok($"Recording started for slot {slotNumber}. Click again to stop.");
        }
        catch (Exception ex)
        {
            return DigitalVoiceKeyerOperationResult.Fail(ex.Message);
        }
    }

    private DigitalVoiceKeyerOperationResult StopRecordingLocked(string logTypeKey, int slotNumber)
    {
        StopRecordingProcessLocked();
        UpdateRecordingState(logTypeKey, slotNumber, false, GetRecordingPath(logTypeKey, slotNumber));
        return DigitalVoiceKeyerOperationResult.Ok($"Recording stopped for slot {slotNumber}.");
    }

    private void StopRecordingProcessLocked()
    {
        var process = _activeRecordingProcess;
        _activeRecordingProcess = null;
        _activeRecordingKey = null;

        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            process.WaitForExit(2000);
        }
        catch
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    private void UpdateRecordingState(string logTypeKey, int slotNumber, bool isRecording, string recordingPath)
    {
        var config = AppConfigurationStore.Load();
        var bankWasCreated = false;
        var bank = GetOrCreateBank(config, logTypeKey, ref bankWasCreated);
        var record = bank.Records.First(x => x.SlotNumber == slotNumber);
        record.IsRecording = isRecording;
        record.RecordingPath = recordingPath;
        AppConfigurationStore.Save(config);
        RaiseBankChanged();
    }

    private static bool TryStartRecordingProcess(string fileName, string arguments, out Process? process)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start())
            {
                process.Dispose();
                process = null;
                return false;
            }

            return true;
        }
        catch
        {
            process?.Dispose();
            process = null;
            return false;
        }
    }

    private static DigitalVoiceKeyerBankConfig GetOrCreateBank(AppConfiguration config, string logTypeKey, ref bool wasCreated)
    {
        config.DigitalVoiceKeyer ??= new DigitalVoiceKeyerConfiguration();
        config.DigitalVoiceKeyer.Banks ??= new Dictionary<string, DigitalVoiceKeyerBankConfig>(StringComparer.OrdinalIgnoreCase);

        if (!config.DigitalVoiceKeyer.Banks.TryGetValue(logTypeKey, out var bank) || bank is null)
        {
            bank = new DigitalVoiceKeyerBankConfig
            {
                LogTypeKey = logTypeKey,
                Records = BuildDefaultRecords(logTypeKey)
            };

            config.DigitalVoiceKeyer.Banks[logTypeKey] = bank;
            wasCreated = true;
        }

        bank.LogTypeKey = string.IsNullOrWhiteSpace(bank.LogTypeKey) ? logTypeKey : bank.LogTypeKey.Trim();
        bank.Records = NormalizeConfigRecords(logTypeKey, bank.Records);

        return bank;
    }

    private static int NormalizeSlot(int slotNumber)
        => Math.Clamp(slotNumber, 1, SlotsPerBank);

    private static List<DigitalVoiceKeyerRecordConfig> BuildDefaultRecords(string logTypeKey)
    {
        var records = new List<DigitalVoiceKeyerRecordConfig>(SlotsPerBank);
        for (var slot = 1; slot <= SlotsPerBank; slot++)
        {
            records.Add(new DigitalVoiceKeyerRecordConfig
            {
                SlotNumber = slot,
                Label = BuildDefaultLabel(logTypeKey, slot),
                Message = string.Empty,
                RecordingPath = string.Empty,
                IsRecording = false
            });
        }

        return records;
    }

    private static List<DigitalVoiceKeyerRecordConfig> NormalizeInputRecords(
        string logTypeKey,
        IEnumerable<DigitalVoiceKeyerRecord>? records)
    {
        var map = new Dictionary<int, DigitalVoiceKeyerRecordConfig>();

        foreach (var record in records ?? [])
        {
            if (record is null)
                continue;

            var slot = NormalizeSlot(record.SlotNumber);
            map[slot] = new DigitalVoiceKeyerRecordConfig
            {
                SlotNumber = slot,
                Label = (record.Label ?? string.Empty).Trim(),
                Message = (record.Message ?? string.Empty).Trim(),
                RecordingPath = (record.RecordingPath ?? string.Empty).Trim(),
                IsRecording = record.IsRecording
            };
        }

        return BuildNormalizedConfigRecords(logTypeKey, map);
    }

    private static List<DigitalVoiceKeyerRecordConfig> NormalizeConfigRecords(
        string logTypeKey,
        IEnumerable<DigitalVoiceKeyerRecordConfig>? records)
    {
        var map = new Dictionary<int, DigitalVoiceKeyerRecordConfig>();

        foreach (var record in records ?? [])
        {
            if (record is null)
                continue;

            var slot = NormalizeSlot(record.SlotNumber);
            map[slot] = new DigitalVoiceKeyerRecordConfig
            {
                SlotNumber = slot,
                Label = (record.Label ?? string.Empty).Trim(),
                Message = (record.Message ?? string.Empty).Trim(),
                RecordingPath = (record.RecordingPath ?? string.Empty).Trim(),
                IsRecording = record.IsRecording
            };
        }

        return BuildNormalizedConfigRecords(logTypeKey, map);
    }

    private static List<DigitalVoiceKeyerRecordConfig> BuildNormalizedConfigRecords(
        string logTypeKey,
        IReadOnlyDictionary<int, DigitalVoiceKeyerRecordConfig> map)
    {
        var normalized = new List<DigitalVoiceKeyerRecordConfig>(SlotsPerBank);
        for (var slot = 1; slot <= SlotsPerBank; slot++)
        {
            if (!map.TryGetValue(slot, out var record))
            {
                normalized.Add(new DigitalVoiceKeyerRecordConfig
                {
                    SlotNumber = slot,
                    Label = BuildDefaultLabel(logTypeKey, slot),
                    Message = string.Empty,
                    RecordingPath = string.Empty,
                    IsRecording = false
                });
                continue;
            }

            normalized.Add(new DigitalVoiceKeyerRecordConfig
            {
                SlotNumber = slot,
                Label = string.IsNullOrWhiteSpace(record.Label) ? BuildDefaultLabel(logTypeKey, slot) : record.Label,
                Message = record.Message,
                RecordingPath = record.RecordingPath,
                IsRecording = record.IsRecording
            });
        }

        return normalized;
    }

    private static string NormalizeLogTypeKey(string? logTypeKey)
        => string.IsNullOrWhiteSpace(logTypeKey) ? ContestCatalog.NormalKey : logTypeKey.Trim().ToUpperInvariant();

    private static string MakeSessionKey(string logTypeKey, int slotNumber)
        => $"{NormalizeLogTypeKey(logTypeKey)}:{NormalizeSlot(slotNumber):00}";

    private static string BuildDefaultLabel(string logTypeKey, int slot)
    {
        var definition = ContestCatalog.GetByKey(logTypeKey) ?? ContestCatalog.Get(ContestType.Normal);
        return $"{definition.DisplayName} {slot}";
    }

    private static string GetRecordingPath(string logTypeKey, int slot)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var baseDir = Path.Combine(homeDir, "HamBusLog");
        var folder = Path.Combine(baseDir, RecordingFolderName, logTypeKey);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"slot-{slot:00}.wav");
    }

    private static string GetConfiguredPlaybackDevice()
    {
        var config = AppConfigurationStore.Load();
        return NormalizePlaybackDevice(config.DigitalVoiceKeyer.OutputDevice);
    }

    private static string NormalizePlaybackDevice(string? deviceName)
    {
        var trimmed = deviceName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed)
            || string.Equals(trimmed, SystemDefaultPlaybackDevice, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return trimmed;
    }

    private static IEnumerable<(string FileName, string Arguments)> BuildPlaybackAttempts(string slotPath, string preferredDevice)
    {
        var escapedPath = EscapeArgument(slotPath);
        if (!string.IsNullOrWhiteSpace(preferredDevice))
        {
            var escapedDevice = EscapeArgument(preferredDevice);
            yield return ("pw-play", $"--target \"{escapedDevice}\" \"{escapedPath}\"");
            yield return ("aplay", $"-D \"{escapedDevice}\" \"{escapedPath}\"");
        }

        yield return ("pw-play", $"\"{escapedPath}\"");
        yield return ("aplay", $"\"{escapedPath}\"");
    }

    private static IEnumerable<string> EnumeratePactlSinks()
    {
        if (!TryRunProcessCapture("pactl", "list short sinks", out var output))
            return [];

        var devices = new List<string>();
        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = rawLine.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                devices.Add(parts[1]);
        }

        return devices;
    }

    private static IEnumerable<string> EnumerateAlsaPlaybackDevices()
    {
        if (!TryRunProcessCapture("aplay", "-L", out var output))
            return [];

        var devices = new List<string>();
        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(rawLine) || char.IsWhiteSpace(rawLine[0]))
                continue;

            var candidate = rawLine.Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
                devices.Add(candidate);
        }

        return devices;
    }

    private static bool TryRunProcessCapture(string fileName, string arguments, out string output)
    {
        output = string.Empty;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
                return false;

            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string EscapeArgument(string value)
        => value.Replace("\"", "\\\"");

    private static async Task<ProcessRunResult> RunPlaybackAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Failed to start process '{fileName}'.");

        try
        {
            var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return new ProcessRunResult(process.ExitCode, await stdOutTask, await stdErrTask);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            throw;
        }
    }

    private void RaiseBankChanged() => BankChanged?.Invoke(this, EventArgs.Empty);

    private readonly record struct ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);
}















