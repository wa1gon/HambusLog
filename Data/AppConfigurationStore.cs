namespace HamBusLog.Data;

public static class AppConfigurationStore
{
    public const int MinimumRigRadioCount = 8;
    private const string DefaultRigTagPrefix = "radio-";

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
                return new AppConfiguration();
            }

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
            config ??= new AppConfiguration();
            EnsureActiveProfile(config, json);

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
        EnsureActiveProfile(config, null);
        config.Rigctld ??= new RigctldConfiguration();
        EnsureRigctldRadios(config.Rigctld);
        return config.Rigctld;
    }

    public static RigctldRadioConfiguration GetActiveRigctldRadio(AppConfiguration config)
    {
        var rigctld = GetRigctld(config);
        var activeTag = rigctld.Radios.FirstOrDefault(x => x.IsActive)?.TagName;
        if (string.IsNullOrWhiteSpace(activeTag))
            activeTag = rigctld.ActiveRadioTags.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(activeTag))
            activeTag = rigctld.ActiveRadioTag;
        return GetRigctldRadio(rigctld, activeTag);
    }

    public static IReadOnlyList<RigctldRadioConfiguration> GetActiveRigctldRadios(AppConfiguration config)
    {
        var rigctld = GetRigctld(config);
        var tags = rigctld.Radios
            .Where(x => x.IsActive)
            .Select(x => NormalizeTagName(x.TagName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tags.Count == 0)
        {
            tags = rigctld.ActiveRadioTags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeTagName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (tags.Count == 0)
            tags.Add(NormalizeTagName(rigctld.ActiveRadioTag));

        var radios = tags.Select(tag => GetRigctldRadio(rigctld, tag)).ToList();
        rigctld.ActiveRadioTags = radios.Select(x => x.TagName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (rigctld.ActiveRadioTags.Count > 0)
            rigctld.ActiveRadioTag = rigctld.ActiveRadioTags[0];
        return radios;
    }

    public static RigctldRadioConfiguration GetRigctldRadio(AppConfiguration config, int radioId)
    {
        var rigctld = GetRigctld(config);
        return GetRigctldRadio(rigctld, radioId);
    }

    public static RigctldRadioConfiguration GetRigctldRadio(AppConfiguration config, string? tagName)
    {
        var rigctld = GetRigctld(config);
        return GetRigctldRadio(rigctld, tagName);
    }

    public static RigctldRadioConfiguration GetRigctldRadio(RigctldConfiguration rigctld, int radioId)
    {
        var tag = $"{DefaultRigTagPrefix}{(radioId <= 0 ? 1 : radioId)}";
        return GetRigctldRadio(rigctld, tag);
    }

    public static RigctldRadioConfiguration GetRigctldRadio(RigctldConfiguration rigctld, string? tagName)
    {
        EnsureRigctldRadios(rigctld);
        var normalizedTag = NormalizeTagName(tagName);
        var radio = rigctld.Radios.FirstOrDefault(x => string.Equals(x.TagName, normalizedTag, StringComparison.OrdinalIgnoreCase));
        if (radio is null)
        {
            radio = CreateDefaultRigctldRadio(rigctld.Radios.Count + 1);
            radio.TagName = normalizedTag;
            rigctld.Radios.Add(radio);
            rigctld.Radios = rigctld.Radios.OrderBy(x => ExtractTagSequence(x.TagName)).ThenBy(x => x.TagName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        rigctld.ActiveRadioTag = radio.TagName;
        rigctld.ActiveRadioId = ExtractTagSequence(radio.TagName);
        if (rigctld.ActiveRadioTags.Count == 0)
            rigctld.ActiveRadioTags = [radio.TagName];
        MirrorActiveRadioToLegacyFields(rigctld, radio);
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

        EnsureRigctldConfiguration(config, rawJson);
    }

    private static void EnsureRigctldConfiguration(AppConfiguration config, string? rawJson)
    {
        config.Rigctld ??= new RigctldConfiguration();

        // Migrate legacy profile-scoped Rigctld settings only when top-level Rigctld is absent in raw JSON.
        if (!ContainsTopLevelRigctldProperty(rawJson))
        {
            RigctldConfiguration? legacy = null;
            if (config.Profiles.TryGetValue(config.ActiveProfile, out var activeProfile))
                legacy = activeProfile.LegacyRigctld;

            legacy ??= config.Profiles.Values.Select(x => x.LegacyRigctld).FirstOrDefault(x => x is not null);
            if (legacy is not null)
            {
                config.Rigctld = new RigctldConfiguration
                {
                    Host = legacy.Host,
                    Port = legacy.Port,
                    SerialPortName = legacy.SerialPortName,
                    RiglistFilePath = legacy.RiglistFilePath,
                    ActiveRigNum = legacy.ActiveRigNum
                };
            }
        }

        // Clear legacy profile property so newly saved config keeps Rigctld only at top level.
        foreach (var profile in config.Profiles.Values)
            profile.LegacyRigctld = null;

        EnsureRigctldRadios(config.Rigctld);
        var activeRadio = GetRigctldRadio(config.Rigctld, config.Rigctld.ActiveRadioTag);
        MirrorActiveRadioToLegacyFields(config.Rigctld, activeRadio);
    }

    private static void EnsureRigctldRadios(RigctldConfiguration rigctld)
    {
        rigctld.Radios ??= [];
        var existing = rigctld.Radios.Where(x => x is not null).ToList();
        rigctld.Radios = [];
        foreach (var radio in existing)
        {
            var candidateTag = NormalizeTagName(string.IsNullOrWhiteSpace(radio.TagName)
                ? $"{DefaultRigTagPrefix}{(radio.RadioId <= 0 ? 1 : radio.RadioId)}"
                : radio.TagName);
            if (rigctld.Radios.Any(x => string.Equals(x.TagName, candidateTag, StringComparison.OrdinalIgnoreCase)))
                continue;

            radio.TagName = candidateTag;
            if (radio.RadioId <= 0)
                radio.RadioId = ExtractTagSequence(candidateTag);
            rigctld.Radios.Add(radio);
        }

        // Migrate legacy single-radio fields into first slot when no explicit radios exist.
        if (rigctld.Radios.Count == 0)
        {
            var migrated = CreateDefaultRigctldRadio(1);
            migrated.Executable = string.IsNullOrWhiteSpace(rigctld.Executable) ? migrated.Executable : rigctld.Executable;
            migrated.ArgumentsTemplate = string.IsNullOrWhiteSpace(rigctld.ArgumentsTemplate) ? migrated.ArgumentsTemplate : rigctld.ArgumentsTemplate;
            migrated.Host = string.IsNullOrWhiteSpace(rigctld.Host) ? migrated.Host : rigctld.Host;
            migrated.Port = rigctld.Port <= 0 ? migrated.Port : rigctld.Port;
            migrated.SerialPortName = rigctld.SerialPortName ?? string.Empty;
            migrated.RiglistFilePath = rigctld.RiglistFilePath ?? string.Empty;
            migrated.ActiveRigNum = rigctld.ActiveRigNum;
            rigctld.Radios.Add(migrated);
        }

        for (var i = 1; i <= MinimumRigRadioCount; i++)
        {
            var tag = $"{DefaultRigTagPrefix}{i}";
            if (rigctld.Radios.All(x => !string.Equals(x.TagName, tag, StringComparison.OrdinalIgnoreCase)))
                rigctld.Radios.Add(CreateDefaultRigctldRadio(i));
        }

        rigctld.Radios = rigctld.Radios
            .OrderBy(x => ExtractTagSequence(x.TagName))
            .ThenBy(x => x.TagName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        EnsureUniqueRigctldPorts(rigctld);

        NormalizeActiveRadioFlags(rigctld);
    }

    private static RigctldRadioConfiguration CreateDefaultRigctldRadio(int radioId) => new()
    {
        TagName = $"{DefaultRigTagPrefix}{radioId}",
        RadioId = radioId,
        DisplayName = $"Radio {radioId}"
    };

    private static void MirrorActiveRadioToLegacyFields(RigctldConfiguration rigctld, RigctldRadioConfiguration radio)
    {
        radio.ActiveRigNums ??= [];
        if (radio.ActiveRigNums.Count == 0 && radio.ActiveRigNum is int activeSingle)
            radio.ActiveRigNums = [activeSingle];
        if (radio.ActiveRigNum is null && radio.ActiveRigNums.Count > 0)
            radio.ActiveRigNum = radio.ActiveRigNums[0];

        rigctld.Executable = radio.Executable;
        rigctld.ArgumentsTemplate = radio.ArgumentsTemplate;
        rigctld.Host = radio.Host;
        rigctld.Port = radio.Port;
        rigctld.SerialPortName = radio.SerialPortName;
        rigctld.RiglistFilePath = radio.RiglistFilePath;
        rigctld.ActiveRigNum = radio.ActiveRigNum ?? radio.ActiveRigNums.FirstOrDefault();
    }

    private static string NormalizeTagName(string? tagName)
    {
        var trimmed = (tagName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return $"{DefaultRigTagPrefix}1";

        return trimmed;
    }

    private static int ExtractTagSequence(string? tagName)
    {
        var tag = tagName ?? string.Empty;
        var match = Regex.Match(tag, @"(\d+)$");
        if (!match.Success)
            return 1;
        return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : 1;
    }

    private static void EnsureUniqueRigctldPorts(RigctldConfiguration rigctld)
    {
        var used = new HashSet<int>();
        var next = 4532;
        foreach (var radio in rigctld.Radios)
        {
            var port = radio.Port;
            if (port <= 0 || used.Contains(port))
            {
                while (used.Contains(next))
                    next++;
                port = next;
            }

            used.Add(port);
            radio.Port = port;
            if (port == next)
                next++;
        }
    }

    private static void NormalizeActiveRadioFlags(RigctldConfiguration rigctld)
    {
        if (string.IsNullOrWhiteSpace(rigctld.ActiveRadioTag))
            rigctld.ActiveRadioTag = $"{DefaultRigTagPrefix}{(rigctld.ActiveRadioId <= 0 ? 1 : rigctld.ActiveRadioId)}";
        rigctld.ActiveRadioTag = NormalizeTagName(rigctld.ActiveRadioTag);
        if (rigctld.Radios.All(x => !string.Equals(x.TagName, rigctld.ActiveRadioTag, StringComparison.OrdinalIgnoreCase)))
            rigctld.ActiveRadioTag = rigctld.Radios[0].TagName;

        rigctld.ActiveRadioTags ??= [];
        var activeTagSet = rigctld.ActiveRadioTags
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeTagName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(tag => rigctld.Radios.Any(r => string.Equals(r.TagName, tag, StringComparison.OrdinalIgnoreCase)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var flaggedActive = rigctld.Radios.Where(x => x.IsActive).Select(x => x.TagName);
        foreach (var tag in flaggedActive)
            activeTagSet.Add(tag);

        if (activeTagSet.Count == 0)
            activeTagSet.Add(rigctld.ActiveRadioTag);

        rigctld.ActiveRadioTags = rigctld.Radios
            .Where(x => activeTagSet.Contains(x.TagName))
            .Select(x => x.TagName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var radio in rigctld.Radios)
            radio.IsActive = activeTagSet.Contains(radio.TagName);

        if (rigctld.ActiveRadioTags.Count == 0)
            rigctld.ActiveRadioTags.Add(rigctld.Radios[0].TagName);

        rigctld.ActiveRadioTag = rigctld.ActiveRadioTags[0];
        rigctld.ActiveRadioId = ExtractTagSequence(rigctld.ActiveRadioTag);
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

    private static bool ContainsTopLevelRigctldProperty(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return doc.RootElement.TryGetProperty("Rigctld", out _);
        }
        catch
        {
            return false;
        }
    }
}
