namespace HamBusLog.Models;

public sealed class AppConfiguration
{
    public string ActiveProfile { get; set; } = "default";
    public Dictionary<string, ConfigProfile> Profiles { get; set; } = new()
    {
        { "default", new ConfigProfile { Name = "default" } }
    };
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
    public string InputBackgroundColor { get; set; } = "#2C3E50";
    public string InputForegroundColor { get; set; } = "#FFFFFF";
    public string InputBorderColor { get; set; } = "#34495E";
    public string InputSelectionBackgroundColor { get; set; } = "#2C3E50";
    public string InputSelectionForegroundColor { get; set; } = "#FFFFFF";
    public string ConnectionString { get; set; } = "Data Source=hambuslog.db";
    public RigctldConfiguration Rigctld { get; set; } = new();
}

public sealed class RigctldConfiguration
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4532;
    public string SerialPortName { get; set; } = string.Empty;
    public string RiglistFilePath { get; set; } = string.Empty;
    public int? ActiveRigNum { get; set; }

    // Multi-radio support
    public string ActiveRadioTag { get; set; } = "radio-1";
    public List<string> ActiveRadioTags { get; set; } = [];
    public List<RigRadioConfig> Radios { get; set; } = [];
}

public sealed class RigRadioConfig
{
    public int RadioId { get; set; }
    public string TagName { get; set; } = "radio-1";
    public string DisplayName { get; set; } = "radio-1";
    public string Executable { get; set; } = "rigctld";
    public string ArgumentsTemplate { get; set; } = "-m {rigNum} -T {host} -t {port}{serialArg}";
    public string AdditionalArguments { get; set; } = string.Empty;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4532;
    public string SerialPortName { get; set; } = string.Empty;
    public string RiglistFilePath { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
