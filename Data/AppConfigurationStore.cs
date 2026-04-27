namespace HamBusLog.Data;

public static class AppConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "hambuslog.json");

    public static string GetConfigFilePath() => ConfigFilePath;

    public static AppConfiguration Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                var fresh = new AppConfiguration();
                EnsureRigConfiguration(fresh);
                return fresh;
            }

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
            config ??= new AppConfiguration();
            EnsureActiveProfile(config, json);
            EnsureRigConfiguration(config);

            if (!ContainsActiveProfileProperty(json))
                Save(config);

            return config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppConfigurationStore.Load error: {ex}");
            return new AppConfiguration();
        }
    }

    public static void Save(AppConfiguration configuration)
    {
        try
        {
            EnsureActiveProfile(configuration, null);
            EnsureRigConfiguration(configuration);

            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(configuration, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
            System.Diagnostics.Debug.WriteLine($"AppConfigurationStore.Save success: {ConfigFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppConfigurationStore.Save error: {ex}");
            throw;
        }
    }

    public static ConfigProfile GetActiveProfile(AppConfiguration config)
    {
        EnsureActiveProfile(config, null);
        if (!config.Profiles.TryGetValue(config.ActiveProfile, out var profile))
        {
            profile = new ConfigProfile { Name = config.ActiveProfile };
            config.Profiles[config.ActiveProfile] = profile;
        }
        return profile;
    }

    public static RigctldConfiguration GetRigctld(AppConfiguration config)
    {
        var profile = GetActiveProfile(config);
        profile.Rigctld ??= new RigctldConfiguration();
        NormalizeRigctld(profile.Rigctld);
        return profile.Rigctld;
    }

    public static RigRadioConfig GetRigctldRadio(RigctldConfiguration rigctld, string? requestedTag)
    {
        NormalizeRigctld(rigctld);
        var tag = string.IsNullOrWhiteSpace(requestedTag) ? rigctld.ActiveRadioTag : requestedTag.Trim();

        var radio = rigctld.Radios.FirstOrDefault(x => string.Equals(x.TagName, tag, StringComparison.OrdinalIgnoreCase));
        if (radio is null)
        {
            var nextId = rigctld.Radios.Count == 0 ? 1 : rigctld.Radios.Max(x => x.RadioId) + 1;
            radio = new RigRadioConfig
            {
                RadioId = nextId,
                TagName = tag,
                DisplayName = $"Radio {nextId}",
                Host = string.IsNullOrWhiteSpace(rigctld.Host) ? "127.0.0.1" : rigctld.Host,
                Port = rigctld.Port <= 0 ? 4532 : rigctld.Port,
                SerialPortName = rigctld.SerialPortName,
                RiglistFilePath = rigctld.RiglistFilePath,
                IsActive = true
            };
            rigctld.Radios.Add(radio);
        }

        rigctld.ActiveRadioTag = radio.TagName;
        if (!rigctld.ActiveRadioTags.Contains(radio.TagName, StringComparer.OrdinalIgnoreCase))
            rigctld.ActiveRadioTags.Add(radio.TagName);

        NormalizeRigctld(rigctld);
        return radio;
    }

    private static void EnsureActiveProfile(AppConfiguration config, string? rawJson)
    {
        if (config.Profiles.Count == 0)
            config.Profiles["default"] = new ConfigProfile { Name = "default" };

        if (string.IsNullOrWhiteSpace(config.ActiveProfile))
            config.ActiveProfile = ExtractLegacyCurrentProfile(rawJson) ?? "default";

        if (!config.Profiles.ContainsKey(config.ActiveProfile))
            config.Profiles[config.ActiveProfile] = new ConfigProfile { Name = config.ActiveProfile };
    }

    private static string? ExtractLegacyCurrentProfile(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (!doc.RootElement.TryGetProperty("ActiveProfile", out var activeElement))
            {
                if (!doc.RootElement.TryGetProperty("CurrentProfile", out activeElement))
                    return null;
            }

            var active = activeElement.GetString();
            return string.IsNullOrWhiteSpace(active) ? null : active.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsActiveProfileProperty(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return doc.RootElement.TryGetProperty("ActiveProfile", out _);
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureRigConfiguration(AppConfiguration config)
    {
        foreach (var profile in config.Profiles.Values)
        {
            profile.Rigctld ??= new RigctldConfiguration();
            NormalizeRigctld(profile.Rigctld);
        }
    }

    private static void NormalizeRigctld(RigctldConfiguration rigctld)
    {
        rigctld.Host = string.IsNullOrWhiteSpace(rigctld.Host) ? "127.0.0.1" : rigctld.Host.Trim();
        if (rigctld.Port <= 0)
            rigctld.Port = 4532;

        rigctld.ActiveRadioTags ??= [];
        rigctld.Radios ??= [];

        if (rigctld.Radios.Count == 0)
        {
            var seedTag = string.IsNullOrWhiteSpace(rigctld.ActiveRadioTag) ? "radio-1" : rigctld.ActiveRadioTag.Trim();
            rigctld.Radios.Add(new RigRadioConfig
            {
                RadioId = 1,
                TagName = seedTag,
                DisplayName = "Radio 1",
                Host = rigctld.Host,
                Port = rigctld.Port,
                SerialPortName = rigctld.SerialPortName,
                RiglistFilePath = rigctld.RiglistFilePath,
                IsActive = true
            });
        }

        var normalizedRadios = new List<RigRadioConfig>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var id = 1;
        foreach (var radio in rigctld.Radios)
        {
            var tag = string.IsNullOrWhiteSpace(radio.TagName) ? $"radio-{id}" : radio.TagName.Trim();
            if (!seenTags.Add(tag))
                continue;

            radio.RadioId = radio.RadioId <= 0 ? id : radio.RadioId;
            radio.TagName = tag;
            radio.DisplayName = string.IsNullOrWhiteSpace(radio.DisplayName) ? tag : radio.DisplayName.Trim();
            radio.Executable = string.IsNullOrWhiteSpace(radio.Executable) ? "rigctld" : radio.Executable.Trim();
            radio.ArgumentsTemplate = string.IsNullOrWhiteSpace(radio.ArgumentsTemplate)
                ? "-m {rigNum} -T {host} -t {port}{serialArg}"
                : radio.ArgumentsTemplate;
            radio.AdditionalArguments = string.IsNullOrWhiteSpace(radio.AdditionalArguments)
                ? string.Empty
                : radio.AdditionalArguments.Trim();
            radio.Host = string.IsNullOrWhiteSpace(radio.Host) ? rigctld.Host : radio.Host.Trim();
            radio.Port = radio.Port <= 0 ? rigctld.Port : radio.Port;

            if (string.IsNullOrWhiteSpace(radio.SerialPortName))
                radio.SerialPortName = rigctld.SerialPortName;
            if (string.IsNullOrWhiteSpace(radio.RiglistFilePath))
                radio.RiglistFilePath = rigctld.RiglistFilePath;

            normalizedRadios.Add(radio);
            id++;
        }

        rigctld.Radios = normalizedRadios;

        if (string.IsNullOrWhiteSpace(rigctld.ActiveRadioTag))
            rigctld.ActiveRadioTag = rigctld.Radios[0].TagName;

        var tagSet = rigctld.ActiveRadioTags
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var activeRadio in rigctld.Radios.Where(x => x.IsActive))
            tagSet.Add(activeRadio.TagName);

        if (tagSet.Count == 0)
            tagSet.Add(rigctld.ActiveRadioTag);

        rigctld.ActiveRadioTags = tagSet.ToList();

        foreach (var radio in rigctld.Radios)
            radio.IsActive = rigctld.ActiveRadioTags.Contains(radio.TagName, StringComparer.OrdinalIgnoreCase);

        if (!rigctld.ActiveRadioTags.Contains(rigctld.ActiveRadioTag, StringComparer.OrdinalIgnoreCase))
            rigctld.ActiveRadioTag = rigctld.ActiveRadioTags[0];
    }
}
