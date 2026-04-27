namespace HamBusLog.Models;

public sealed class AppConfiguration
{
    public string ActiveProfile { get; set; } = "default";
    public Dictionary<string, ConfigProfile> Profiles { get; set; } = new()
    {
        { "default", new ConfigProfile { Name = "default" } }
    };

    public RigctldConfiguration Rigctld { get; set; } = new();
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
    public string ButtonCautionColor { get; set; } = "#D97706";
    public string ButtonDangerColor { get; set; } = "#DC2626";
    public string ButtonForegroundColor { get; set; } = "#FFFFFF";
    public string ConnectionString { get; set; } = "Data Source=hambuslog.db";

    // Legacy location for Rigctld settings (profile-scoped). Read only for migration.
    [JsonPropertyName("Rigctld")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RigctldConfiguration? LegacyRigctld { get; set; }
}

public sealed class RigctldConfiguration
{
    public string ActiveRadioTag { get; set; } = "radio-1";
    public List<string> ActiveRadioTags { get; set; } = [];
    // Legacy numeric active radio id; kept for migration.
    public int ActiveRadioId { get; set; } = 1;
    public List<RigctldRadioConfiguration> Radios { get; set; } = [];

    // Legacy single-radio fields kept for backward compatibility/migration.
    public string Executable { get; set; } = "rigctld";
    public string ArgumentsTemplate { get; set; } = "-m {rigNum} -T {host} -t {port}{serialArg}";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4532;
    public string SerialPortName { get; set; } = string.Empty;
    public string RiglistFilePath { get; set; } = string.Empty;
    public int? ActiveRigNum { get; set; }
}

public sealed class RigctldRadioConfiguration
{
    public string TagName { get; set; } = string.Empty;
    // Legacy numeric radio id; kept for migration.
    public int RadioId { get; set; }
    public bool IsActive { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Executable { get; set; } = "rigctld";
    public string ArgumentsTemplate { get; set; } = "-m {rigNum} -T {host} -t {port}{serialArg}";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4532;
    public string SerialPortName { get; set; } = string.Empty;
    public string RiglistFilePath { get; set; } = string.Empty;
    public int? ActiveRigNum { get; set; }
    public List<int> ActiveRigNums { get; set; } = [];
}
