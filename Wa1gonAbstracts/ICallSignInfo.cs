namespace HamBusLog.Wa1gonLib;

public interface ICallSignInfo
{
    public string CallSign { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    public string State { get; set; }
    public string County { get; set; }
    public string Grid { get; set; }
    public int Dxcc { get; set; }
    public int Itu { get; set; }

    public int Cq { get; set; }
    // Add more properties as needed
}