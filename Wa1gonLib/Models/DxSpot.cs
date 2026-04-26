

namespace HamBusLog.Wa1gonLib.Models;

public class DxSpot
{
    public string? Spotter { get; set; }
    public double Frequency { get; set; }
    public string? Callsign { get; set; }
    public string? Info { get; set; }
    public DateTime Timestamp { get; set; }

    public static DxSpot? ParseSpot(string line)
    {
        // Example: DX de DK9BTX:    14235.0  YO9LIG       CQ CQ CQ DX                    1911Z
        var match = Regex.Match(
            line,
            @"DX de (\w+):\s+([\d\.]+)\s+([A-Z0-9/]+)\s+(.+?)\s+(\d{4}Z)",
            RegexOptions.IgnoreCase
        );
        if (!match.Success) return null;

        var spotter = match.Groups[1].Value;
        var freq = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var callsign = match.Groups[3].Value;
        var info = match.Groups[4].Value.Trim();
        var timeStr = match.Groups[5].Value;

        // Parse time (HHmmZ)
        var now = DateTime.UtcNow;
        var hour = int.Parse(timeStr.Substring(0, 2));
        var min = int.Parse(timeStr.Substring(2, 2));
        var timestamp = new DateTime(now.Year, now.Month, now.Day, hour, min, 0, DateTimeKind.Utc);

        return new DxSpot
        {
            Spotter = spotter,
            Frequency = freq,
            Callsign = callsign,
            Info = info,
            Timestamp = timestamp
        };
    }

    public override string ToString()
    {
        // Format timestamp as HHmmZ (UTC)
        var timeStr = Timestamp.ToUniversalTime().ToString("HHmm'Z'");
        // Pad frequency to match cluster output (e.g., 14340.0)
        var freqStr = Frequency.ToString("F1", CultureInfo.InvariantCulture);
        // Info may be empty
        return $"DX de {Spotter}:    {freqStr}  {Callsign}   {Info}   {timeStr}".Trim();
    }
}
