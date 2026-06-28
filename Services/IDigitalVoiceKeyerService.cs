namespace HamBusLog.Services;

public interface IDigitalVoiceKeyerService
{
    IReadOnlyList<AudioPlaybackDeviceOption> GetAvailablePlaybackDevices();

    string GetPreferredPlaybackDevice();

    void SetPreferredPlaybackDevice(string? deviceName);

    bool GetCompactViewEnabled();

    void SetCompactViewEnabled(bool enabled);

    IReadOnlyList<DigitalVoiceKeyerRecord> GetRecordsForLogType(string? logTypeKey);

    void SaveRecordsForLogType(string? logTypeKey, IEnumerable<DigitalVoiceKeyerRecord> records);

    Task<DigitalVoiceKeyerOperationResult> RecordSlotAsync(string? logTypeKey, int slotNumber, CancellationToken cancellationToken = default);

    Task<DigitalVoiceKeyerOperationResult> PlaySlotAsync(string? logTypeKey, int slotNumber, CancellationToken cancellationToken = default);

    Task<DigitalVoiceKeyerOperationResult> TestSlotAsync(string? logTypeKey, int slotNumber, CancellationToken cancellationToken = default);

    bool DeleteRecording(string? logTypeKey, int slotNumber);

    event EventHandler? BankChanged;
}

public sealed class DigitalVoiceKeyerRecord
{
    public int SlotNumber { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int RepeatDelaySeconds { get; init; }
    public string RecordingPath { get; init; } = string.Empty;
    public bool HasRecording { get; init; }
    public bool IsRecording { get; init; }
}

public readonly record struct DigitalVoiceKeyerOperationResult(bool Success, string Message)
{
    public static DigitalVoiceKeyerOperationResult Ok(string message) => new(true, message);
    public static DigitalVoiceKeyerOperationResult Fail(string message) => new(false, message);
}

public sealed record AudioPlaybackDeviceOption(string Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}




