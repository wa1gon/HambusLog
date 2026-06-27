namespace HamBusLog.ViewModels;

using Avalonia.Threading;
using System.ComponentModel;

public sealed class DigitalVoiceKeyerViewModel : ViewModelBase, IDisposable
{
    private readonly IDigitalVoiceKeyerService _voiceKeyerService;
    private readonly ILogTypeSelectionService _logTypeSelectionService;
    private readonly ObservableCollection<DigitalVoiceKeyerRowViewModel> _rows = [];
    private readonly ObservableCollection<string> _availableOutputDevices = [];

    private string _selectedLogTypeDisplayName = "Normal";
    private string _selectedOutputDevice = DigitalVoiceKeyerService.SystemDefaultPlaybackDevice;
    private string _statusMessage = "Ready";

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
        RefreshOutputDevices();
        LoadFromGlobalLogType();
    }

    public ObservableCollection<DigitalVoiceKeyerRowViewModel> Rows => _rows;

    public ObservableCollection<string> AvailableOutputDevices => _availableOutputDevices;

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

    public string SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? DigitalVoiceKeyerService.SystemDefaultPlaybackDevice
                : value.Trim();
            if (!SetProperty(ref _selectedOutputDevice, normalized))
                return;

            _voiceKeyerService.SetPreferredPlaybackDevice(normalized);
            StatusMessage = string.Equals(normalized, DigitalVoiceKeyerService.SystemDefaultPlaybackDevice, StringComparison.Ordinal)
                ? "Using system default playback device."
                : $"Using playback device: {normalized}";
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

    public async Task PlaySlotAsync(int slotNumber)
    {
        var logType = _logTypeSelectionService.GetSelectedContestDefinition();
        var result = await _voiceKeyerService.PlaySlotAsync(logType.Key, slotNumber);
        StatusMessage = result.Message;
    }

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

        _availableOutputDevices.Clear();
        foreach (var device in devices)
            _availableOutputDevices.Add(device);

        if (!_availableOutputDevices.Contains(selected, StringComparer.OrdinalIgnoreCase))
            _availableOutputDevices.Add(selected);

        _selectedOutputDevice = selected;
        OnPropertyChanged(nameof(SelectedOutputDevice));
    }

    private void OnSelectedContestChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            LoadFromGlobalLogType();
            StatusMessage = $"Loaded bank for {SelectedLogTypeDisplayName}.";
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
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
                record.RecordingPath,
                record.HasRecording,
                record.IsRecording));
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

    public DigitalVoiceKeyerRowViewModel(int slotNumber, string label, string message, string recordingPath, bool hasRecording, bool isRecording)
    {
        SlotNumber = slotNumber;
        _label = label;
        _message = message;
        _recordingPath = recordingPath;
        _hasRecording = hasRecording;
        _isRecording = isRecording;
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

    public string RecordingFileName => string.IsNullOrWhiteSpace(_recordingPath) ? string.Empty : Path.GetFileName(_recordingPath);

    public string RecordButtonText => IsRecording ? "Stop" : "Record";

    public event PropertyChangedEventHandler? PropertyChanged;
}



