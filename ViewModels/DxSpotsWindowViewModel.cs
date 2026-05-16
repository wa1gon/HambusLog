namespace HamBusLog.ViewModels;

using Avalonia.Threading;

public sealed class DxSpotsWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IDxSpotFeed _spotFeed;
    private readonly ObservableCollection<DxSpot> _spots = [];
    private readonly List<DxSpot> _allSpots = [];
    private string _spotSummary = "DX spots: 0";
    private SpotRegionOption _selectedRegion = SpotRegionOption.All;

    public DxSpotsWindowViewModel()
        : this(App.DxSpotFeed)
    {
    }

    internal DxSpotsWindowViewModel(IDxSpotFeed spotFeed)
    {
        _spotFeed = spotFeed;
        foreach (var spot in _spotFeed.GetSnapshot().OrderByDescending(x => x.Timestamp))
            _allSpots.Add(spot);

        TrimToConfiguredQueueLength();
        ApplyFilter();
        _spotFeed.SpotReceived += OnSpotReceived;
    }

    public ObservableCollection<DxSpot> Spots => _spots;

    public IReadOnlyList<SpotRegionOption> RegionFilters { get; } =
    [
        SpotRegionOption.All,
        SpotRegionOption.NorthAmerica,
        SpotRegionOption.SouthAmerica,
        SpotRegionOption.Europe,
        SpotRegionOption.Africa,
        SpotRegionOption.Asia,
        SpotRegionOption.Oceania,
        SpotRegionOption.Antarctica
    ];

    public SpotRegionOption SelectedRegion
    {
        get => _selectedRegion;
        set
        {
            if (!SetProperty(ref _selectedRegion, value))
                return;

            ApplyFilter();
        }
    }

    public string SpotSummary
    {
        get => _spotSummary;
        private set => SetProperty(ref _spotSummary, value);
    }

    private void OnSpotReceived(object? sender, DxSpot spot)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            AddSpot(spot);
            return;
        }

        Dispatcher.UIThread.Post(() => AddSpot(spot));
    }

    private void AddSpot(DxSpot spot)
    {
        _allSpots.Insert(0, spot);
        TrimToConfiguredQueueLength();

        if (IsSpotVisible(spot, _selectedRegion))
            _spots.Insert(0, spot);

        UpdateSummary();
    }

    private void TrimToConfiguredQueueLength()
    {
        var max = GetConfiguredQueueLength();
        while (_allSpots.Count > max)
            _allSpots.RemoveAt(_allSpots.Count - 1);
    }

    private void UpdateSummary()
    {
        SpotSummary = $"DX spots: {_spots.Count}/{_allSpots.Count}";
    }

    private void ApplyFilter()
    {
        _spots.Clear();
        foreach (var spot in _allSpots)
        {
            if (IsSpotVisible(spot, _selectedRegion))
                _spots.Add(spot);
        }

        UpdateSummary();
    }

    private static bool IsSpotVisible(DxSpot spot, SpotRegionOption region)
    {
        if (region.Region == SpotRegion.All)
            return true;

        var callsign = NormalizeCallsign(spot.Spotter);
        if (string.IsNullOrWhiteSpace(callsign))
            return false;

        var resolved = ResolveRegion(callsign);
        return resolved == region.Region;
    }

    private static string NormalizeCallsign(string? callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
            return string.Empty;

        var primary = callsign.Trim().ToUpperInvariant();
        var slashIndex = primary.IndexOf('/');
        if (slashIndex > 0)
            primary = primary[..slashIndex];

        return primary;
    }

    private static SpotRegion ResolveRegion(string callsign)
    {
        foreach (var entry in DxRegionPrefixCatalog.GetPrefixes())
        {
            if (entry.Value.Any(prefix => callsign.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return entry.Key;
        }

        return SpotRegion.Unknown;
    }

    private static int GetConfiguredQueueLength()
    {
        var config = AppConfigurationStore.Load();
        var max = config.Cluster?.QueueLength ?? 500;
        return max <= 0 ? 500 : max;
    }

    public void Dispose()
    {
        _spotFeed.SpotReceived -= OnSpotReceived;
    }
}

public sealed class SpotRegionOption
{
    private SpotRegionOption(string label, SpotRegion region)
    {
        Label = label;
        Region = region;
    }

    public string Label { get; }
    public SpotRegion Region { get; }

    public override string ToString() => Label;

    public static SpotRegionOption All { get; } = new("All", SpotRegion.All);
    public static SpotRegionOption NorthAmerica { get; } = new("NA", SpotRegion.NorthAmerica);
    public static SpotRegionOption SouthAmerica { get; } = new("SA", SpotRegion.SouthAmerica);
    public static SpotRegionOption Europe { get; } = new("EU", SpotRegion.Europe);
    public static SpotRegionOption Africa { get; } = new("AF", SpotRegion.Africa);
    public static SpotRegionOption Asia { get; } = new("AS", SpotRegion.Asia);
    public static SpotRegionOption Oceania { get; } = new("OC", SpotRegion.Oceania);
    public static SpotRegionOption Antarctica { get; } = new("AN", SpotRegion.Antarctica);
}

public enum SpotRegion
{
    All,
    NorthAmerica,
    SouthAmerica,
    Europe,
    Africa,
    Asia,
    Oceania,
    Antarctica,
    Unknown
}

