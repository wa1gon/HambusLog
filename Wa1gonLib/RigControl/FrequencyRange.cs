namespace HamBusLog.Wa1gonLib.RigControl;

public class FrequencyRange
{
    public long StartHz { get; set; }
    public long EndHz { get; set; }
    public List<string> VfoList { get; set; } = new();
    public List<string> ModeList { get; set; } = new();
    public List<string> AntennaList { get; set; } = new();
    public string LowPower { get; set; } = string.Empty;
    public string HighPower { get; set; } = string.Empty;
}
