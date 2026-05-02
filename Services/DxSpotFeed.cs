namespace HamBusLog.Services;

public sealed class DxSpotFeed : IDxSpotFeed
{
    private readonly object _sync = new();
    private readonly List<DxSpot> _spots = [];

    public event EventHandler<DxSpot>? SpotReceived;

    public void Publish(DxSpot spot)
    {
        if (spot is null)
            return;

        lock (_sync)
        {
            _spots.Add(spot);
            var max = GetConfiguredQueueLength();
            while (_spots.Count > max)
                _spots.RemoveAt(0);
        }

        SpotReceived?.Invoke(this, spot);
    }

    public bool PublishLine(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
            return false;

        var parsed = DxSpot.ParseSpot(rawLine);
        if (parsed is null)
            return false;

        Publish(parsed);
        return true;
    }

    public IReadOnlyList<DxSpot> GetSnapshot()
    {
        lock (_sync)
            return _spots.ToList();
    }

    private static int GetConfiguredQueueLength()
    {
        var config = AppConfigurationStore.Load();
        var max = config.Cluster?.QueueLength ?? 500;
        return max <= 0 ? 500 : max;
    }
}

