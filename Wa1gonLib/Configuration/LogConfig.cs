namespace HamBusLog.Wa1gonLib.Models;

public record LogConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ProfileName { get; set; } = string.Empty; //= ValidateString(ProfileName, nameof(ProfileName));

    // public string Callsign { get; set; } = ValidateString(Callsign, nameof(Callsign)).ToUpper();
    public string Callsign { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public string GridSquare { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string CountyCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public int Dxcc { get; set; } = 0;
    public int ProKey { get; set; } = 0;
    public bool IsDirty { get; set; } = false; // No [NotMapped], configured in Fluent API
    public ICollection<RigCtlConf> RigControls { get; set; } = [];
    public ICollection<CallBookConf> Logbooks { get; set; } = [];
    public ICollection<DxClusterConf> DxClusters { get; set; } = [];


    private static string ValidateString(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine($"Warning: {propertyName} is null or empty, setting to default.");
            return "Unknown";
        }

        return value;
    }

    public LogConfig Copy()
    {
        return this with
        {
            Logbooks = Logbooks.Select(cb => cb with { }).ToList(),
            RigControls = RigControls.Select(rc => rc with { }).ToList(),
            DxClusters = DxClusters.Select(dc => dc with { }).ToList()
        };
    }
}
