namespace HamBusLog.Wa1gonLib.RigControl;

public class RigCapabilities
{
    public string ModelName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string HamlibVersion { get; set; } = string.Empty;
    public string BackendVersion { get; set; } = string.Empty;
    public string BackendCopyright { get; set; } = string.Empty;
    public string BackendStatus { get; set; } = string.Empty;
    public string RigType { get; set; } = string.Empty;
    public string PttType { get; set; } = string.Empty;
    public string DcdType { get; set; } = string.Empty;
    public string PortType { get; set; } = string.Empty;
    public string WriteDelay { get; set; } = string.Empty;
    public string PostWriteDelay { get; set; } = string.Empty;
    public bool HasTargetableVfo { get; set; }
    public string TargetableFeatures { get; set; } = string.Empty;
    public bool HasAsyncDataSupport { get; set; }
    public string Announce { get; set; } = string.Empty;
    public string MaxRit { get; set; } = string.Empty;
    public string MaxXit { get; set; } = string.Empty;
    public string MaxIfShift { get; set; } = string.Empty;
    public string Preamp { get; set; } = string.Empty;
    public string Attenuator { get; set; } = string.Empty;
    public string AgcLevels { get; set; } = string.Empty;
    public string Ctcss { get; set; } = string.Empty;
    public string Dcs { get; set; } = string.Empty;
    public string GetFunctions { get; set; } = string.Empty;
    public string SetFunctions { get; set; } = string.Empty;
    public List<string> ModeList { get; set; } = new();
    public List<string> VfoList { get; set; } = new();
    public List<FrequencyRange> TxRanges { get; set; } = new();
    public List<FrequencyRange> RxRanges { get; set; } = new();

    public static RigCapabilities Parse(ImmutableArray<string> lines)
    {
        var caps = new RigCapabilities();
        List<FrequencyRange>? currentRanges = null;
        FrequencyRange? currentRange = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("Model name:"))
            {
                caps.ModelName = trimmed["Model name:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Mfg name:"))
            {
                caps.Manufacturer = trimmed["Mfg name:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Hamlib version:"))
            {
                caps.HamlibVersion = trimmed["Hamlib version:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Backend version:"))
            {
                caps.BackendVersion = trimmed["Backend version:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Backend copyright:"))
            {
                caps.BackendCopyright = trimmed["Backend copyright:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Backend status:"))
            {
                caps.BackendStatus = trimmed["Backend status:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Rig type:"))
            {
                caps.RigType = trimmed["Rig type:".Length..].Trim();
            }
            else if (trimmed.StartsWith("PTT type:"))
            {
                caps.PttType = trimmed["PTT type:".Length..].Trim();
            }
            else if (trimmed.StartsWith("DCD type:"))
            {
                caps.DcdType = trimmed["DCD type:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Port type:"))
            {
                caps.PortType = trimmed["Port type:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Write delay:"))
            {
                caps.WriteDelay = trimmed["Write delay:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Post write delay:"))
            {
                caps.PostWriteDelay = trimmed["Post write delay:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Has targetable VFO:"))
            {
                caps.HasTargetableVfo = trimmed.Contains("Y");
            }
            else if (trimmed.StartsWith("Targetable features:"))
            {
                caps.TargetableFeatures = trimmed["Targetable features:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Has async data support:"))
            {
                caps.HasAsyncDataSupport = trimmed.Contains("Y");
            }
            else if (trimmed.StartsWith("Announce:"))
            {
                caps.Announce = trimmed["Announce:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Max RIT:"))
            {
                caps.MaxRit = trimmed["Max RIT:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Max XIT:"))
            {
                caps.MaxXit = trimmed["Max XIT:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Max IF-SHIFT:"))
            {
                caps.MaxIfShift = trimmed["Max IF-SHIFT:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Preamp:"))
            {
                caps.Preamp = trimmed["Preamp:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Attenuator:"))
            {
                caps.Attenuator = trimmed["Attenuator:".Length..].Trim();
            }
            else if (trimmed.StartsWith("AGC levels:"))
            {
                caps.AgcLevels = trimmed["AGC levels:".Length..].Trim();
            }
            else if (trimmed.StartsWith("CTCSS:"))
            {
                caps.Ctcss = trimmed["CTCSS:".Length..].Trim();
            }
            else if (trimmed.StartsWith("DCS:"))
            {
                caps.Dcs = trimmed["DCS:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Get functions:"))
            {
                caps.GetFunctions = trimmed["Get functions:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Set functions:"))
            {
                caps.SetFunctions = trimmed["Set functions:".Length..].Trim();
            }
            else if (trimmed.StartsWith("Mode list:"))
            {
                caps.ModeList = trimmed["Mode list:".Length..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
            else if (trimmed.StartsWith("VFO list:"))
            {
                caps.VfoList = trimmed["VFO list:".Length..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            // Frequency ranges
            else if (trimmed.StartsWith("TX ranges"))
            {
                currentRanges = caps.TxRanges;
                currentRange = null!;
            }
            else if (trimmed.StartsWith("RX ranges"))
            {
                currentRanges = caps.RxRanges;
                currentRange = null!;
            }
            else if (currentRanges != null && trimmed.EndsWith("Hz") && trimmed.Contains("-"))
            {
                // Example: "1810000 Hz - 2000000 Hz"
                var parts = trimmed.Split(new[] { "Hz -", "Hz" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 &&
                    long.TryParse(parts[0].Trim().Replace("Hz", "").Replace(" ", ""), out var start) &&
                    long.TryParse(parts[1].Trim().Replace("Hz", "").Replace(" ", ""), out var end))
                {
                    currentRange = new FrequencyRange { StartHz = start, EndHz = end };
                    currentRanges.Add(currentRange);
                }
            }
            else if (currentRange != null)
            {
                if (trimmed.StartsWith("VFO list:"))
                    currentRange.VfoList = trimmed["VFO list:".Length..].Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                else if (trimmed.StartsWith("Mode list:"))
                    currentRange.ModeList = trimmed["Mode list:".Length..].Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                else if (trimmed.StartsWith("Antenna list:"))
                    currentRange.AntennaList = trimmed["Antenna list:".Length..].Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                else if (trimmed.StartsWith("Low power:"))
                    currentRange.LowPower = trimmed["Low power:".Length..].Trim();
                else if (trimmed.StartsWith("High power:"))
                    currentRange.HighPower = trimmed["High power:".Length..].Trim();
                else if (string.IsNullOrWhiteSpace(trimmed))
                    currentRange = null!; // End of current range block
            }
        }

        return caps;
    }
}
