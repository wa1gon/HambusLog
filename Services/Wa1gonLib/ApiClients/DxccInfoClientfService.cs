using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace HamBusLog.Wa1gonLib.ApiClients;
/// <summary>
///     The DxccEntity list will be static for many years if not decades.
/// </summary>
/// <param name="httpClient"></param>
/// <param name="logger"></param>
public class DxccInfoClientService(IHttpClientFactory httpClientFactory, ILogger<DxccInfoClientService> logger)
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("SharedClient");
    private readonly ILogger<DxccInfoClientService> _logger = logger;
    private List<DxccEntity>? _dxccList = [];

    private JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Fetch the entire DXCC entity list.
    ///     Adjust the endpoint path if your API differs.
    /// </summary>
    public async Task<List<DxccEntity>> GetAllAsync(CancellationToken ct = default)
    {
        if (_dxccList is not null && _dxccList.Any())
        {
            _logger.LogDebug("Returning cached DXCC list with {Count} entries.", _dxccList.Count);
            return _dxccList;
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _logger.LogDebug("Fetching DXCC list from API endpoint 'dxcc'.");
        _dxccList = await httpClient.GetFromJsonAsync<List<DxccEntity>>("dxcc", _jsonOptions, ct);

        return _dxccList ?? new List<DxccEntity>();
    }
}

