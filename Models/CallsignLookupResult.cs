namespace HamBusLog.Models;

public sealed class CallsignLookupResult
{
    public string Provider { get; set; } = string.Empty;
    public string CallSign { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string Grid { get; set; } = string.Empty;
    public int? Dxcc { get; set; }
    public int? ItuZone { get; set; }
    public int? CqZone { get; set; }
}

