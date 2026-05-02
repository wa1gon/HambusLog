namespace HamBusLog.Services;

public interface IDxSpotFeed
{
    event EventHandler<DxSpot>? SpotReceived;

    /// <summary>
    /// Publishes a parsed spot to subscribers and stores it in the rolling queue.
    /// </summary>
    void Publish(DxSpot spot);

    /// <summary>
    /// Tries to parse and publish a raw DX cluster line.
    /// </summary>
    bool PublishLine(string rawLine);

    /// <summary>
    /// Returns a snapshot of spots currently retained in memory.
    /// </summary>
    IReadOnlyList<DxSpot> GetSnapshot();
}

