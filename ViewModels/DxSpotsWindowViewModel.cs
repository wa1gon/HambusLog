namespace HamBusLog.ViewModels;

using Avalonia.Threading;

public sealed class DxSpotsWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IDxSpotFeed _spotFeed;
    private readonly ObservableCollection<DxSpot> _spots = [];
    private string _spotSummary = "DX spots: 0";

    public DxSpotsWindowViewModel()
        : this(App.DxSpotFeed)
    {
    }

    internal DxSpotsWindowViewModel(IDxSpotFeed spotFeed)
    {
        _spotFeed = spotFeed;
        foreach (var spot in _spotFeed.GetSnapshot().OrderByDescending(x => x.Timestamp))
            _spots.Add(spot);

        TrimToConfiguredQueueLength();
        UpdateSummary();
        _spotFeed.SpotReceived += OnSpotReceived;
    }

    public ObservableCollection<DxSpot> Spots => _spots;

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
        _spots.Insert(0, spot);
        TrimToConfiguredQueueLength();
        UpdateSummary();
    }

    private void TrimToConfiguredQueueLength()
    {
        var max = GetConfiguredQueueLength();
        while (_spots.Count > max)
            _spots.RemoveAt(_spots.Count - 1);
    }

    private void UpdateSummary()
    {
        SpotSummary = $"DX spots: {_spots.Count}";
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

