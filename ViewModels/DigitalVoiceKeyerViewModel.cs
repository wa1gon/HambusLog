namespace HamBusLog.ViewModels;

using Avalonia.Threading;
using System.ComponentModel;

public sealed class DigitalVoiceKeyerViewModel : ViewModelBase, IDisposable
{
    private readonly IDigitalVoiceKeyerService _voiceKeyerService;
    private readonly ILogTypeSelectionService _logTypeSelectionService;
    private readonly ObservableCollection<DigitalVoiceKeyerRowViewModel> _rows = [];
    private readonly ObservableCollection<AudioPlaybackDeviceOption> _availableOutputDevices = [];
    private readonly ObservableCollection<ContestDefinition> _availableLogTypes = [];

    private string _selectedLogTypeDisplayName = "Normal";
    private AudioPlaybackDeviceOption _selectedOutputDevice = new(string.Empty, DigitalVoiceKeyerService.SystemDefaultPlaybackDevice);
    private ContestDefinition? _selectedLogType;
    private string _statusMessage = "Ready";
    private bool _isCompactView;
    private bool _isApplyingLogTypeFromService;

    public DigitalVoiceKeyerViewModel()
        : this(App.DigitalVoiceKeyerService, App.LogTypeSelectionService)
    {
    }

    internal DigitalVoiceKeyerViewModel(
        IDigitalVoiceKeyerService voiceKeyerService,
        ILogTypeSelectionService logTypeSelectionService)
    {
        _voiceKeyerService = voiceKeyerService;
        _logTypeSelectionService = logTypeSelectionService;

        _voiceKeyerService.BankChanged += OnBankChanged;
        _logTypeSelectionService.SelectedContestChanged += OnSelectedContestChanged;
        RefreshAvailableLogTypes();
        ApplySelectedLogTypeFromService();
        RefreshOutputDevices();
        IsCompactView = _voiceKeyerService.GetCompactViewEnabled();
        LoadFromGlobalLogType();
    }

    public ObservableCollection<DigitalVoiceKeyerRowViewModel> Rows => _rows;

    public ObservableCollection<AudioPlaybackDeviceOption> AvailableOutputDevices => _availableOutputDevices;

    public ObservableCollection<ContestDefinition> AvailableLogTypes => _availableLogTypes;

    public ContestDefinition? SelectedLogType
    {
        get => _selectedLogType;
        set
        {
            if (!SetProperty(ref _selectedLogType, value))
                return;

            if (_isApplyingLogTypeFromService || value is null)
                return;

            _logTypeSelectionService.SetSelectedContestKey(value.Key);
        }
    }

    public string SelectedLogTypeDisplayName
    {
        get => _selectedLogTypeDisplayName;
        private set => SetProperty(ref _selectedLogTypeDisplayName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (!SetProperty(ref _isCompactView, value))
                return;

            _voiceKeyerService.SetCompactViewEnabled(value);
            OnPropertyChanged(nameof(IsDetailedView));
            foreach (var row in _rows)
                row.IsCompactView = value;
        }
    }

    public bool IsDetailedView => !IsCompactView;

    public AudioPlaybackDeviceOption SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set
        {
            var normalized = NormalizeOutputDevice(value);
            if (!SetProperty(ref _selectedOutputDevice, normalized))
                return;

            _voiceKeyerService.SetPreferredPlaybackDevice(normalized.Value);
            StatusMessage = string.IsNullOrWhiteSpace(normalized.Value)
                ? "Using system default playback device."
                : $"Using playback device: {normalized.DisplayName}";
        }
    }

    public void SaveCurrentBank()
    {
        var logType = _logTypeSelectionService.GetSelectedContestDefinition();
        var records = _rows.Select(x => new DigitalVoiceKeyerRecord
        {
            SlotNumber = x.SlotNumber,
            Label = x.Label,
            Message = x.Message,
            RepeatDelaySeconds = x.RepeatDelaySeconds,
            RecordingPath = x.RecordingPath,
            HasRecording = x.HasRecording,
            IsRecording = x.IsRecording
        });

        _voiceKeyerService.SaveRecordsForLogType(logType.Key, records);
        StatusMessage = $"Saved 10 records for {logType.DisplayName}.";
    }

    public async Task RecordSlotAsync(int slotNumber)
    {
        var logType = _logTypeSelectionService.GetSelectedContestDefinition();
        var result = await _voiceKeyerService.RecordSlotAsync(logType.Key, slotNumber);
        StatusMessage = result.Message;
        LoadFromGlobalLogType();
    }

    public async Task<DigitalVoiceKeyerOperationResult> PlaySlotAsync(int slotNumber, CancellationToken cancellationToken = default)
    {
        var logType = _logTypeSelectionService.GetSelectedContestDefinition();
        var result = await _voiceKeyerService.PlaySlotAsync(logType.Key, slotNumber, cancellationToken);
        StatusMessage = result.Message;
        return result;
    }

    public async Task<DigitalVoiceKeyerOperationResult> TestSlotAsync(int slotNumber, CancellationToken cancellationToken = default)
    {
        var logType = _logTypeSelectionService.GetSelectedContestDefinition();
        var result = await _voiceKeyerService.TestSlotAsync(logType.Key, slotNumber, cancellationToken);
        StatusMessage = result.Message;
        return result;
    }

    public void SetStatusMessage(string message)
        => StatusMessage = message?.Trim() ?? string.Empty;

    public void DeleteRecording(int slotNumber)
    {
        var logType = _logTypeSelectionService.GetSelectedContestDefinition();
        var deleted = _voiceKeyerService.DeleteRecording(logType.Key, slotNumber);
        StatusMessage = deleted
            ? $"Deleted recording for slot {slotNumber}."
            : $"Could not delete recording for slot {slotNumber}.";
        LoadFromGlobalLogType();
    }

    public void ReloadCurrentBank()
    {
        RefreshOutputDevices();
        LoadFromGlobalLogType();
        StatusMessage = $"Reloaded records for {SelectedLogTypeDisplayName}.";
    }

    private void RefreshOutputDevices()
    {
        var devices = _voiceKeyerService.GetAvailablePlaybackDevices();
        var selected = _voiceKeyerService.GetPreferredPlaybackDevice();
        var normalizedSelectedValue = NormalizePlaybackDeviceValue(selected);

        _availableOutputDevices.Clear();
        foreach (var device in devices)
            _availableOutputDevices.Add(device);

        var selectedOption = FindOutputDevice(normalizedSelectedValue)
            ?? CreateCanonicalOutputDevice(normalizedSelectedValue);

        if (!_availableOutputDevices.Any(x => string.Equals(x.Value, selectedOption.Value, StringComparison.OrdinalIgnoreCase)))
            _availableOutputDevices.Add(selectedOption);

        _selectedOutputDevice = selectedOption;
        OnPropertyChanged(nameof(SelectedOutputDevice));
    }

    private AudioPlaybackDeviceOption NormalizeOutputDevice(AudioPlaybackDeviceOption? option)
    {
        var normalizedValue = NormalizePlaybackDeviceValue(option?.Value);
        if (string.IsNullOrWhiteSpace(normalizedValue))
            return CreateCanonicalOutputDevice(string.Empty);

        return new AudioPlaybackDeviceOption(
            normalizedValue,
            string.IsNullOrWhiteSpace(option?.DisplayName)
                ? DigitalVoiceKeyerService.BuildPlaybackDeviceDisplayName(normalizedValue)
                : option!.DisplayName.Trim());
    }

    private AudioPlaybackDeviceOption? FindOutputDevice(string? value)
    {
        var normalized = NormalizePlaybackDeviceValue(value);
        return _availableOutputDevices.FirstOrDefault(x => string.Equals(x.Value, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static AudioPlaybackDeviceOption CreateCanonicalOutputDevice(string rawValue)
    {
        var normalized = NormalizePlaybackDeviceValue(rawValue);
        return new AudioPlaybackDeviceOption(
            normalized,
            string.IsNullOrWhiteSpace(normalized)
                ? DigitalVoiceKeyerService.SystemDefaultPlaybackDevice
                : DigitalVoiceKeyerService.BuildPlaybackDeviceDisplayName(normalized));
    }

    private static string NormalizePlaybackDeviceValue(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized)
            || string.Equals(normalized, DigitalVoiceKeyerService.SystemDefaultPlaybackDevice, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return normalized;
    }

    private void RefreshAvailableLogTypes()
    {
        _availableLogTypes.Clear();
        foreach (var contest in _logTypeSelectionService.GetAvailableContests())
            _availableLogTypes.Add(contest);
    }

    private void ApplySelectedLogTypeFromService()
    {
        var selected = _availableLogTypes.FirstOrDefault(x =>
            string.Equals(x.Key, _logTypeSelectionService.SelectedContestKey, StringComparison.OrdinalIgnoreCase))
            ?? _logTypeSelectionService.GetSelectedContestDefinition();

        _isApplyingLogTypeFromService = true;
        try
        {
            SelectedLogType = selected;
        }
        finally
        {
            _isApplyingLogTypeFromService = false;
        }
    }

    private void OnSelectedContestChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplySelectedLogTypeFromService();
            LoadFromGlobalLogType();
            StatusMessage = $"Loaded bank for {SelectedLogTypeDisplayName}.";
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            ApplySelectedLogTypeFromService();
            LoadFromGlobalLogType();
            StatusMessage = $"Loaded bank for {SelectedLogTypeDisplayName}.";
        });
    }

    private void OnBankChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            LoadFromGlobalLogType();
            return;
        }

        Dispatcher.UIThread.Post(LoadFromGlobalLogType);
    }

    private void LoadFromGlobalLogType()
    {
        var contest = _logTypeSelectionService.GetSelectedContestDefinition();
        var records = _voiceKeyerService.GetRecordsForLogType(contest.Key)
            .OrderBy(x => x.SlotNumber)
            .ToList();

        SelectedLogTypeDisplayName = contest.DisplayName;

        _rows.Clear();
        foreach (var record in records)
            _rows.Add(new DigitalVoiceKeyerRowViewModel(
                record.SlotNumber,
                record.Label,
                record.Message,
                record.RepeatDelaySeconds,
                record.RecordingPath,
                record.HasRecording,
                record.IsRecording,
                IsCompactView));
    }

    public void Dispose()
    {
        _voiceKeyerService.BankChanged -= OnBankChanged;
        _logTypeSelectionService.SelectedContestChanged -= OnSelectedContestChanged;
    }
}

public sealed class DigitalVoiceKeyerRowViewModel : INotifyPropertyChanged
{
    private string _label;
    private string _message;
    private string _recordingPath;
    private bool _hasRecording;
    private bool _isRecording;
    private bool _isPlaying;
    private int _repeatDelaySeconds;
    private bool _isCompactView;

    public DigitalVoiceKeyerRowViewModel(int slotNumber, string label, string message, int repeatDelaySeconds, string recordingPath, bool hasRecording, bool isRecording, bool isCompactView)
    {
        SlotNumber = slotNumber;
        _label = label;
        _message = message;
        _recordingPath = recordingPath;
        _hasRecording = hasRecording;
        _isRecording = isRecording;
        _isPlaying = false;
        _repeatDelaySeconds = Math.Max(0, repeatDelaySeconds);
        _isCompactView = isCompactView;
    }

    public int SlotNumber { get; }

    public string Label
    {
        get => _label;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_label, normalized, StringComparison.Ordinal))
                return;

            _label = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_message, normalized, StringComparison.Ordinal))
                return;

            _message = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
        }
    }

    public string RecordingPath
    {
        get => _recordingPath;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_recordingPath, normalized, StringComparison.Ordinal))
                return;

            _recordingPath = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecordingPath)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecordingFileName)));
        }
    }

    public bool HasRecording
    {
        get => _hasRecording;
        set
        {
            if (_hasRecording == value)
                return;

            _hasRecording = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasRecording)));
        }
    }

    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (_isRecording == value)
                return;

            _isRecording = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRecording)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecordButtonText)));
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value)
                return;

            _isPlaying = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayButtonText)));
        }
    }

    public int RepeatDelaySeconds
    {
        get => _repeatDelaySeconds;
        set
        {
            var normalized = Math.Max(0, value);
            if (_repeatDelaySeconds == normalized)
                return;

            _repeatDelaySeconds = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RepeatDelaySeconds)));
        }
    }

    public bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (_isCompactView == value)
                return;

            _isCompactView = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompactView)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDetailedView)));
        }
    }

    public bool IsDetailedView => !IsCompactView;

    public string RecordingFileName => string.IsNullOrWhiteSpace(_recordingPath) ? string.Empty : Path.GetFileName(_recordingPath);

    public string RecordButtonText => IsRecording ? "Stop" : "Record";

    public string PlayButtonText => IsPlaying ? "Stop" : "Play";

    public event PropertyChangedEventHandler? PropertyChanged;
}






