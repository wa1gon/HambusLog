namespace HamBusLog.Services;

public interface IDxClusterSpotPublisher
{
    Task<string> SendSpotAsync(DxSpotRequest request, CancellationToken ct = default);
}

public sealed record DxSpotRequest(
    string SpotterCall,
    string TargetCall,
    decimal FrequencyMhz,
    string Comment,
    bool IsSelfSpot);

