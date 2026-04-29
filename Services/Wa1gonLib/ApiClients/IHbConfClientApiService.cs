namespace HamBusLog.Wa1gonLib.ApiClients;

public interface IHbConfClientApiService
{
    Task<List<LogConfig>?> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(LogConfig config,CancellationToken ct = default);
    Task UpdateAsync(LogConfig config,CancellationToken ct = default);
    Task DeleteAsync(string profileId,CancellationToken ct = default);
}

