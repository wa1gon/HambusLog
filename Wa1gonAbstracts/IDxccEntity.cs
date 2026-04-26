namespace HBAbstractions;

public interface IDxccRoot
{
    public List<IDxccEntity> Dxcc { get; set; }
}

public interface IDxccEntity
{
    public List<string> Continent { get; set; }
    public string CountryCode { get; set; }
    public List<int> Cq { get; set; }
    public bool Deleted { get; set; }
    public int EntityCode { get; set; }
    public string Flag { get; set; }
    public List<int> Itu { get; set; }
    public string Name { get; set; }
    public string Notes { get; set; }
    public bool OutgoingQslService { get; set; }
    public string Prefix { get; set; }
    public string PrefixRegex { get; set; }
    public bool ThirdPartyTraffic { get; set; }
    public string ValidEnd { get; set; }
    public string ValidStart { get; set; }
}