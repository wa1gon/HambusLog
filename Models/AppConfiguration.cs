namespace HamBusLog.Models;

public sealed class AppConfiguration
{
    public string ActiveProfile { get; set; } = "default";
    // Legacy field kept for migration from older config files.
    public string LicenseKey { get; set; } = string.Empty;
    public Dictionary<string, WindowPlacement> WindowPlacements { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ContestDefinitionConfig> Contests { get; set; } = [];
    public Dictionary<string, ConfigProfile> Profiles { get; set; } = new()
    {
        { "default", new ConfigProfile { Name = "default" } }
    };

    /// <summary>System-wide radio configuration, shared across all profiles.</summary>
    public RigctldConfiguration Rigctld { get; set; } = new();

    /// <summary>System-wide DX cluster connection settings.</summary>
    public ClusterConfig Cluster { get; set; } = new();

    /// <summary>Callsign lookup provider settings.</summary>
    public CallsignLookupConfiguration CallsignLookup { get; set; } = new();

    /// <summary>Logbook of the World (LoTW) upload settings.</summary>
    public LotwConfiguration Lotw { get; set; } = new();

    /// <summary>System-wide WSJT-X UDP bridge settings.</summary>
    public WsjtConfiguration Wsjt { get; set; } = new();

    /// <summary>System-wide digital voice keyer banks keyed by log type.</summary>
    public DigitalVoiceKeyerConfiguration DigitalVoiceKeyer { get; set; } = new();
}

public sealed class WindowPlacement
{
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class ConfigProfile
{
    public string Name { get; set; } = "default";
    public double AppFontSize { get; set; } = 12.0;
    public string AdifDirectory { get; set; } = string.Empty;
    public string DatabaseFolderPath { get; set; } = string.Empty;
    public string DatabaseFileName { get; set; } = "hambuslog.db";
    public string DatabaseFilePath { get; set; } = string.Empty;
    public string ApplicationLogFolderPath { get; set; } = string.Empty;
    public string ApplicationLogFileName { get; set; } = "hambuslog.log";
    public string ApplicationLogFilePath { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#0F172A";
    public string ForegroundColor { get; set; } = "#E5E7EB";
    public string MenuBackgroundColor { get; set; } = "#111827";
    public string MenuForegroundColor { get; set; } = "#F9FAFB";
    public string ButtonNormalColor { get; set; } = "#2563EB";
    public string ButtonNormalForegroundColor { get; set; } = "#FFFFFF";
    public string ButtonCautionColor { get; set; } = "#B45309";
    public string ButtonCautionForegroundColor { get; set; } = "#FFFFFF";
    public string ButtonDangerColor { get; set; } = "#B91C1C";
    public string ButtonDangerForegroundColor { get; set; } = "#FFFFFF";
    public string ButtonForegroundColor { get; set; } = "#FFFFFF";
    public string InputBackgroundColor { get; set; } = "#1F2937";
    public string InputForegroundColor { get; set; } = "#F9FAFB";
    public string InputBorderColor { get; set; } = "#334155";
    public string InputSelectionBackgroundColor { get; set; } = "#1D4ED8";
    public string InputSelectionForegroundColor { get; set; } = "#FFFFFF";
    public string MutedForegroundColor { get; set; } = "#94A3B8";
    public string HoverFontColor { get; set; } = "#FFFFFF";
    public string ConnectionString { get; set; } = "Data Source=hambuslog.db";

    // ── Station / operator info ──────────────────────────────────────
    public string StationCallSign { get; set; } = string.Empty;
    public string MyLocation { get; set; } = string.Empty;
    public string MyStateProvince { get; set; } = string.Empty;
    public string MyGridSquare { get; set; } = string.Empty;
    public string MyLatitude { get; set; } = string.Empty;
    public string MyLongitude { get; set; } = string.Empty;
    public string MyItuZone { get; set; } = string.Empty;
    public string MyCqZone { get; set; } = string.Empty;
    public string MyFieldDaySection { get; set; } = string.Empty;
    public string MyFieldDayClass { get; set; } = string.Empty;
    public string LastContestKey { get; set; } = string.Empty;
    public bool StayOnTopMainWindow { get; set; }
    public bool StayOnTopLogInputWindow { get; set; }
    public bool StayOnTopAddContactWindow { get; set; }
    public bool StayOnTopArqpProgressWindow { get; set; }
    public bool StayOnTopArrlFdProgressWindow { get; set; }
    public bool StayOnTopDigitalVoiceKeyerWindow { get; set; }
}

public sealed class ContestDefinitionConfig
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AdifContestId { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = "normal";
    public string StartUtc { get; set; } = string.Empty;
    public string EndUtc { get; set; } = string.Empty;
    public List<ContestFieldRequirementConfig> RequiredFields { get; set; } = [];
}

public sealed class ContestFieldRequirementConfig
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DetailFieldName { get; set; } = string.Empty;
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

public sealed class CallsignLookupConfiguration
{
    public QrzLookupConfiguration Qrz { get; set; } = new();
}

public sealed class QrzLookupConfiguration
{
    // Current QRZ login fields.
    public string Username { get; set; } = string.Empty;

    // Legacy fields kept for migration from older config files.
    public string PasswordCiphertext { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("Password")]
    public string? LegacyPassword { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountId
    {
        get => null;
        set { if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Username)) Username = value!.Trim(); }
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApiKey
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value)
                && string.IsNullOrWhiteSpace(PasswordCiphertext)
                && string.IsNullOrWhiteSpace(LegacyPassword))
            {
                LegacyPassword = value.Trim();
            }
        }
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId
    {
        get => null;
        set { if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Username)) Username = value.Trim(); }
    }
}

public sealed class LotwConfiguration
{
    /// <summary>Whether LoTW upload is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Automatically upload each QSO to LoTW when logged.</summary>
    public bool AutoUploadOnLog { get; set; }

    /// <summary>Path to the tqsl executable (defaults to searching PATH).</summary>
    public string TqslPath { get; set; } = "tqsl";

    /// <summary>Command-line template used to download LoTW QSOs to ADIF.
    /// Supports {callsign}, {output}, {from}, and {to} placeholders.
    /// </summary>
    public string DownloadArgumentsTemplate { get; set; } = "-d -c \"{callsign}\" -o \"{output}\"";

    /// <summary>Station callsign certificate to use for signing. Defaults to active profile station callsign.</summary>
    public string StationCallsign { get; set; } = string.Empty;

    /// <summary>XOR-obfuscated TQSL password (same scheme as QRZ).</summary>
    public string PasswordCiphertext { get; set; } = string.Empty;
}

public sealed class WsjtConfiguration
{
    public bool Enabled { get; set; } = true;
    public string ListenAddress { get; set; } = "0.0.0.0";
    public int ListenPort { get; set; } = 2237;
    public bool AcceptOnlyLocalhost { get; set; } = true;
    public bool AutoPopulateLogInput { get; set; } = true;
    public int DebugQueueLength { get; set; } = 500;
}

public sealed class DigitalVoiceKeyerConfiguration
{
    public string OutputDevice { get; set; } = string.Empty;
    public bool CompactView { get; set; }
    public Dictionary<string, DigitalVoiceKeyerBankConfig> Banks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DigitalVoiceKeyerBankConfig
{
    public string LogTypeKey { get; set; } = string.Empty;
    public List<DigitalVoiceKeyerRecordConfig> Records { get; set; } = [];
}

public sealed class DigitalVoiceKeyerRecordConfig
{
    public int SlotNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int RepeatDelaySeconds { get; set; }
    public string RecordingPath { get; set; } = string.Empty;
    public bool IsRecording { get; set; }
}

