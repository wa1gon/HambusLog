namespace HamBusLog.Models;

public sealed class AppConfiguration
{
    public string ActiveProfile { get; set; } = "default";
    public Dictionary<string, WindowPlacement> WindowPlacements { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ConfigProfile> Profiles { get; set; } = new()
    {
        { "default", new ConfigProfile { Name = "default" } }
    };

    /// <summary>System-wide radio configuration, shared across all profiles.</summary>
    public RigctldConfiguration Rigctld { get; set; } = new();
}

public sealed class WindowPlacement
{
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class ConfigProfile
{
    public string Name { get; set; } = "default";
    public string AdifDirectory { get; set; } = string.Empty;
    public string DatabaseFolderPath { get; set; } = string.Empty;
    public string DatabaseFileName { get; set; } = "hambuslog.db";
    public string DatabaseFilePath { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#1F2937";
    public string ForegroundColor { get; set; } = "#FFFFFF";
    public string MenuBackgroundColor { get; set; } = "#111827";
    public string MenuForegroundColor { get; set; } = "#FFFFFF";
    public string ButtonNormalColor { get; set; } = "#2563EB";
    public string ButtonNormalForegroundColor { get; set; } = "#FFFFFF";
    public string ButtonCautionColor { get; set; } = "#D97706";
    public string ButtonCautionForegroundColor { get; set; } = "#FFFFFF";
    public string ButtonDangerColor { get; set; } = "#DC2626";
    public string ButtonDangerForegroundColor { get; set; } = "#FFFFFF";
    public string ButtonForegroundColor { get; set; } = "#FFFFFF";
    public string InputBackgroundColor { get; set; } = "#2C3E50";
    public string InputForegroundColor { get; set; } = "#FFFFFF";
    public string InputBorderColor { get; set; } = "#34495E";
    public string InputSelectionBackgroundColor { get; set; } = "#2C3E50";
    public string InputSelectionForegroundColor { get; set; } = "#FFFFFF";
    public string ConnectionString { get; set; } = "Data Source=hambuslog.db";

    // ── Station / operator info ──────────────────────────────────────
    public string MyCall { get; set; } = string.Empty;
    public string MyLocation { get; set; } = string.Empty;
    public string MyGridSquare { get; set; } = string.Empty;
    public string MyLatitude { get; set; } = string.Empty;
    public string MyLongitude { get; set; } = string.Empty;
    public string MyItuZone { get; set; } = string.Empty;
    public string MyCqZone { get; set; } = string.Empty;
    public string MyFieldDaySection { get; set; } = string.Empty;
    public string MyFieldDayClass { get; set; } = string.Empty;
}

public sealed class RigctldConfiguration
{
    public int ReconnectIntervalSeconds { get; set; } = 3;
    public int? ActiveRigNum { get; set; }
    public string RiglistFilePath { get; set; } = string.Empty;

    // Multi-radio support
    public string ActiveRadioName { get; set; } = string.Empty;
    public List<string> ActiveRadioNames { get; set; } = [];
    public List<RigRadioConfig> Radios { get; set; } = [];

    // ── Migration shims: read old field names from config files written before the rename ──
    // These are never written back (null returned on get → WhenWritingNull suppresses them).

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActiveRadioTag
    {
        get => null;
        set { if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(ActiveRadioName)) ActiveRadioName = value!.Trim(); }
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ActiveRadioTags
    {
        get => null;
        set { if (value?.Count > 0 && ActiveRadioNames.Count == 0) ActiveRadioNames = value; }
    }
}

public sealed class RigRadioConfig
{
    public int RadioId { get; set; }
    public string RadioName { get; set; } = string.Empty;

    // ── Migration shims: read TagName / DisplayName from old config files ──
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TagName
    {
        get => null;
        set { if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(RadioName)) RadioName = value!.Trim(); }
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName
    {
        get => null;
        set { if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(RadioName)) RadioName = value!.Trim(); }
    }
    public string Executable { get; set; } = "rigctld";
    public string ArgumentsTemplate { get; set; } = "-m {rigNum} -T {host} -t {port}{serialArg}";
    public string AdditionalArguments { get; set; } = string.Empty;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4532;
    public string SerialPortName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
