namespace HamBusLog.ViewModels;

using Avalonia.Threading;
using System.Text;

public sealed class WsjtDebugWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IWsjtBridgeService _bridgeService;
    private readonly ObservableCollection<WsjtTrafficRowViewModel> _rows = [];
    private readonly List<WsjtTrafficRowViewModel> _allRows = [];

    private WsjtMessageFilterOption _selectedFilter = WsjtMessageFilterOption.NoStatus;
    private WsjtTrafficRowViewModel? _selectedRow;
    private bool _isPaused;
    private string _summary = "WSJT traffic: 0";

    public WsjtDebugWindowViewModel()
        : this(App.WsjtBridgeService)
    {
    }

    internal WsjtDebugWindowViewModel(IWsjtBridgeService bridgeService)
    {
        _bridgeService = bridgeService;

        foreach (var entry in _bridgeService.GetTrafficSnapshot())
            _allRows.Add(WsjtTrafficRowViewModel.From(entry));

        ApplyFilter();
        _bridgeService.TrafficObserved += OnTrafficObserved;
    }

    public ObservableCollection<WsjtTrafficRowViewModel> Rows => _rows;

    public IReadOnlyList<WsjtMessageFilterOption> Filters { get; } =
    [
        WsjtMessageFilterOption.All,
        WsjtMessageFilterOption.NoStatus,
        WsjtMessageFilterOption.Heartbeat,
        WsjtMessageFilterOption.Status,
        WsjtMessageFilterOption.Decode,
        WsjtMessageFilterOption.LoggedAdif,
        WsjtMessageFilterOption.QsoLogged,
        WsjtMessageFilterOption.Unknown
    ];

    public WsjtMessageFilterOption SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (!SetProperty(ref _selectedFilter, value))
                return;
            ApplyFilter();
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }

    public WsjtTrafficRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (!SetProperty(ref _selectedRow, value))
                return;

            OnPropertyChanged(nameof(SelectedPayloadDecoded));
            OnPropertyChanged(nameof(SelectedPayloadSummary));
        }
    }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public string SelectedPayloadDecoded => SelectedRow?.PayloadDecoded ?? string.Empty;

    public string SelectedPayloadSummary
        => SelectedRow is null
            ? "Select a WSJT message to view the full decoded payload."
            : $"{SelectedRow.MessageType} · {SelectedRow.Summary}";

    public void Clear()
    {
        _allRows.Clear();
        _rows.Clear();
        SelectedRow = null;
        UpdateSummary();
    }

    private void OnTrafficObserved(object? sender, WsjtTrafficEvent e)
    {
        if (Dispatcher.UIThread.CheckAccess()) { AddTraffic(e); return; }
        Dispatcher.UIThread.Post(() => AddTraffic(e));
    }

    private void AddTraffic(WsjtTrafficEvent e)
    {
        var row = WsjtTrafficRowViewModel.From(e);
        _allRows.Insert(0, row);

        var max = GetConfiguredQueueLength();
        while (_allRows.Count > max)
            _allRows.RemoveAt(_allRows.Count - 1);

        if (!IsPaused && IsVisibleForFilter(row, _selectedFilter))
            _rows.Insert(0, row);

        while (_rows.Count > max)
            _rows.RemoveAt(_rows.Count - 1);

        if (SelectedRow is null && _rows.Count > 0)
            SelectedRow = _rows[0];

        UpdateSummary();
    }

    private void ApplyFilter()
    {
        _rows.Clear();
        if (!IsPaused)
        {
            foreach (var row in _allRows)
                if (IsVisibleForFilter(row, _selectedFilter))
                    _rows.Add(row);
        }

        if (_rows.Count == 0)
        {
            SelectedRow = null;
        }
        else if (SelectedRow is null || !_rows.Contains(SelectedRow))
        {
            SelectedRow = _rows[0];
        }

        UpdateSummary();
    }

    private void UpdateSummary() => Summary = $"WSJT traffic: {_rows.Count}/{_allRows.Count}";

    private static int GetConfiguredQueueLength()
    {
        var config = AppConfigurationStore.Load();
        return config.Wsjt.DebugQueueLength <= 0 ? 500 : config.Wsjt.DebugQueueLength;
    }

    private static bool IsVisibleForFilter(WsjtTrafficRowViewModel row, WsjtMessageFilterOption filter)
    {
        if (filter.ExcludedTypes is { } excluded)
            return !excluded.Contains(row.MessageType);
        if (filter.MessageType is null)
            return true;
        return row.MessageType == filter.MessageType.Value;
    }

    public void Dispose() => _bridgeService.TrafficObserved -= OnTrafficObserved;
}

public sealed class WsjtTrafficRowViewModel
{
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Direction { get; init; }
    public required WsjtMessageType MessageType { get; init; }
    public required string ClientId { get; init; }
    public required string Summary { get; init; }
    public required string PayloadDecoded { get; init; }
    public required string PayloadHex { get; init; }

    public static WsjtTrafficRowViewModel From(WsjtTrafficEvent entry)
    {
        return new WsjtTrafficRowViewModel
        {
            TimestampUtc  = entry.TimestampUtc,
            Direction     = entry.Direction,
            MessageType   = entry.MessageType,
            ClientId      = entry.ClientId,
            Summary       = entry.Summary,
            PayloadDecoded = entry.DecodedText,
            PayloadHex    = ToHex(entry.Payload)
        };
    }

    private static string ToHex(byte[] bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return string.Empty;
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}

public sealed class WsjtMessageFilterOption
{
    private WsjtMessageFilterOption(string label,
        WsjtMessageType? messageType = null,
        HashSet<WsjtMessageType>? excludedTypes = null)
    {
        Label         = label;
        MessageType   = messageType;
        ExcludedTypes = excludedTypes;
    }

    public string Label { get; }
    /// <summary>When set, show only this type.</summary>
    public WsjtMessageType? MessageType { get; }
    /// <summary>When set, hide these types (takes priority over MessageType).</summary>
    public HashSet<WsjtMessageType>? ExcludedTypes { get; }

    public override string ToString() => Label;

    public static WsjtMessageFilterOption All { get; }
        = new("All");
    public static WsjtMessageFilterOption NoStatus { get; }
        = new("No Status", excludedTypes: [WsjtMessageType.Status]);
    public static WsjtMessageFilterOption Heartbeat { get; }
        = new("Heartbeat",  WsjtMessageType.Heartbeat);
    public static WsjtMessageFilterOption Status { get; }
        = new("Status",     WsjtMessageType.Status);
    public static WsjtMessageFilterOption Decode { get; }
        = new("Decode",     WsjtMessageType.Decode);
    public static WsjtMessageFilterOption LoggedAdif { get; }
        = new("Logged ADIF", WsjtMessageType.LoggedAdif);
    public static WsjtMessageFilterOption QsoLogged { get; }
        = new("QSO Logged", WsjtMessageType.QsoLogged);
    public static WsjtMessageFilterOption Unknown { get; }
        = new("Unknown",    WsjtMessageType.Unknown);
}

