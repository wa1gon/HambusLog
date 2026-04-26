namespace HBAbstractions;

public interface IDxccInfo
{
    public string CallSign { get; set; }
    public string Name { get; set; }
    public string Details { get; set; }
    public string Continent { get; set; }
    public string Utc { get; set; }
    public int Waz { get; set; }
    public int Dxcc { get; set; }
    public int Itu { get; set; }
    public int Cq { get; set; }
}