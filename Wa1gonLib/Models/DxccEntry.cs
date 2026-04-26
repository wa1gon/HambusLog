namespace HamBusLog.Wa1gonLib.Models;

public class DxccRoot
{
    // [JsonPropertyName("dxcc")]
    public List<DxccEntity> Dxcc { get; set; } = new();
}

public sealed record DxccEntity
{
    public int Id { get; set; }
    public List<string> Continent { get; set; } = new();
    public string CountryCode { get; set; } = string.Empty;
    public List<int> Cq { get; set; } = new();
    public bool Deleted { get; set; }

    public int EntityCode { get; set; }

    // public string Flag { get; set; } = string.Empty;
    public List<int> Itu { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool OutgoingQslService { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public string PrefixRegex { get; set; } = string.Empty;
    public bool ThirdPartyTraffic { get; set; }
    public string ValidEnd { get; set; } = string.Empty;
    public string ValidStart { get; set; } = string.Empty;
}