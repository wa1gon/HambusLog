namespace HamBusLog.Wa1gonLib.ApiClients;

public class HbConfClientApiService : IHbConfClientApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<HbConfClientApiService> _logger;

    public HbConfClientApiService(HttpClient  http, ILogger<HbConfClientApiService> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));;
        _logger = logger;
    }

    public async Task<List<LogConfig>?> GetAllAsync(CancellationToken ct = default)
    {
        // Console.WriteLine("GetAllAsync called");
        // return await _http.GetFromJsonAsync<List<LogConfig>>("conf");
        _logger.LogDebug("GET {Uri}", new Uri(_http.BaseAddress!, "conf"));
        using var resp = await _http.GetAsync("conf", ct);
        resp.EnsureSuccessStatusCode();
        // If no content (204), return empty list
        if (resp.Content.Headers.ContentLength is 0)
            return [];

        var data = await resp.Content.ReadFromJsonAsync<
            List<LogConfig>>(cancellationToken: ct);

        return data ?? [];
    }

    public async Task AddAsync(LogConfig config, CancellationToken ct = default)
    {
        try
        {
            // await _http.PostAsJsonAsync("conf", config);
            Console.WriteLine("Sending POST request to 'conf' with payload:");
            Console.WriteLine(JsonSerializer.Serialize(config));

            var response = await _http.PostAsJsonAsync("conf", config);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("POST request succeeded.");
            }
            else
            {
                Console.WriteLine($"POST request failed with status code: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response content: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred during the POST request:");
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task UpdateAsync(LogConfig config,CancellationToken ct = default)
    {
        await _http.PutAsJsonAsync($"conf/{config.ProfileName}", config);
    }

    public async Task DeleteAsync(string profileId,CancellationToken ct = default)
    {
        await _http.DeleteAsync($"conf/{profileId}");
    }
}
