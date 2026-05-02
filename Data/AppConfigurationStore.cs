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
            EnsureWindowPlacements(config);
            EnsureRigConfiguration(config);
            EnsureClusterConfiguration(config);

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
            EnsureWindowPlacements(configuration);
            EnsureRigConfiguration(configuration);
            EnsureClusterConfiguration(configuration);

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
        EnsureWindowPlacements(config);
        if (!config.Profiles.TryGetValue(config.ActiveProfile, out var profile))
        {
            profile = new ConfigProfile { Name = config.ActiveProfile };
            config.Profiles[config.ActiveProfile] = profile;
        }
        return profile;
    }

    public static RigctldConfiguration GetRigctld(AppConfiguration config)
    {
        config.Rigctld ??= new RigctldConfiguration();
        NormalizeRigctld(config.Rigctld);
        return config.Rigctld;
    }

    public static RigRadioConfig GetRigctldRadio(RigctldConfiguration rigctld, string? requestedRadioName)
    {
        NormalizeRigctld(rigctld);
        var radioName = requestedRadioName is null ? rigctld.ActiveRadioName : requestedRadioName.Trim();

        var radio = rigctld.Radios.FirstOrDefault(x =>
            string.Equals(x.RadioName, radioName, StringComparison.OrdinalIgnoreCase));
        if (radio is null)
        {
            // Do not auto-create for a missing requested name; that can create
            // phantom radios during transient UI selection changes.
            radio = rigctld.Radios.FirstOrDefault(x =>
                        string.Equals(x.RadioName, rigctld.ActiveRadioName, StringComparison.OrdinalIgnoreCase))
                    ?? rigctld.Radios.FirstOrDefault();

            if (radio is null)
            {
                // NormalizeRigctld seeds one radio when empty, but guard defensively.
                radio = new RigRadioConfig
                {
                    RadioId = 1,
                    RadioName = "radio-1",
                    Host = "127.0.0.1",
                    Port = 4532,
                    SerialPortName = string.Empty,
                    IsActive = true
                };
                rigctld.Radios.Add(radio);
            }
        }

        rigctld.ActiveRadioName = radio.RadioName;
        if (!rigctld.ActiveRadioNames.Contains(radio.RadioName, StringComparer.OrdinalIgnoreCase))
            rigctld.ActiveRadioNames.Add(radio.RadioName);

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
        config.Rigctld ??= new RigctldConfiguration();
        NormalizeRigctld(config.Rigctld);
    }

    private static void EnsureClusterConfiguration(AppConfiguration config)
    {
        config.Cluster ??= new ClusterConfig();
        NormalizeCluster(config.Cluster);
    }

    private static void EnsureWindowPlacements(AppConfiguration config)
    {
        config.WindowPlacements ??= new Dictionary<string, WindowPlacement>(StringComparer.OrdinalIgnoreCase);

        var normalized = new Dictionary<string, WindowPlacement>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in config.WindowPlacements)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
                continue;

            normalized[pair.Key.Trim()] = pair.Value;
        }

        config.WindowPlacements = normalized;
    }

    private static void NormalizeRigctld(RigctldConfiguration rigctld)
    {
        if (rigctld.ReconnectIntervalSeconds <= 0)
            rigctld.ReconnectIntervalSeconds = 3;
        if (rigctld.ReconnectIntervalSeconds > 300)
            rigctld.ReconnectIntervalSeconds = 300;
        rigctld.RiglistFilePath = rigctld.RiglistFilePath?.Trim() ?? string.Empty;

        rigctld.ActiveRadioNames ??= [];
        rigctld.Radios ??= [];

        if (rigctld.Radios.Count == 0)
        {
            var seedName = rigctld.ActiveRadioName?.Trim() ?? string.Empty;
            rigctld.Radios.Add(new RigRadioConfig
            {
                RadioId = 1,
                RadioName = seedName,
                Host = "127.0.0.1",
                Port = 4532,
                IsActive = true
            });
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var id = 1;
        foreach (var radio in rigctld.Radios)
        {
            if (radio.RadioId <= 0) radio.RadioId = id;

            var normalizedName = radio.RadioName?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                radio.RadioName = normalizedName;
                var baseName = radio.RadioName;
                var dupIdx = 2;
                while (!seenNames.Add(radio.RadioName))
                {
                    radio.RadioName = $"{baseName}-{dupIdx}";
                    dupIdx++;
                }
            }
            else
            {
                radio.RadioName = string.Empty;
            }

            radio.Host = string.IsNullOrWhiteSpace(radio.Host) ? "127.0.0.1" : radio.Host.Trim();
            radio.Port = radio.Port <= 0 ? 4532 : radio.Port;
            radio.Executable = radio.Executable?.Trim() ?? string.Empty;
            radio.ArgumentsTemplate = radio.ArgumentsTemplate?.Trim() ?? string.Empty;
            radio.AdditionalArguments = radio.AdditionalArguments?.Trim() ?? string.Empty;

            id++;
        }

        if (string.IsNullOrWhiteSpace(rigctld.ActiveRadioName) ||
            !rigctld.Radios.Any(x => string.Equals(x.RadioName, rigctld.ActiveRadioName, StringComparison.OrdinalIgnoreCase)))
            rigctld.ActiveRadioName = rigctld.Radios[0].RadioName;

        var validNames = rigctld.Radios.Select(x => x.RadioName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nameSet = rigctld.ActiveRadioNames
            .Where(x => !string.IsNullOrWhiteSpace(x) && validNames.Contains(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var r in rigctld.Radios.Where(x => x.IsActive))
            nameSet.Add(r.RadioName);

        if (nameSet.Count == 0)
            nameSet.Add(rigctld.ActiveRadioName);

        rigctld.ActiveRadioNames = nameSet.ToList();

        foreach (var radio in rigctld.Radios)
            radio.IsActive = rigctld.ActiveRadioNames.Contains(radio.RadioName, StringComparer.OrdinalIgnoreCase);

        if (!rigctld.ActiveRadioNames.Contains(rigctld.ActiveRadioName, StringComparer.OrdinalIgnoreCase))
            rigctld.ActiveRadioName = rigctld.ActiveRadioNames[0];
    }

    private static void NormalizeCluster(ClusterConfig cluster)
    {
        cluster.Hostname = string.IsNullOrWhiteSpace(cluster.Hostname) ? "127.0.0.1" : cluster.Hostname.Trim();
        cluster.TcpPort = cluster.TcpPort <= 0 ? 7300 : cluster.TcpPort;
        cluster.Callsign = cluster.Callsign?.Trim() ?? string.Empty;
        cluster.Password = cluster.Password ?? string.Empty;
        cluster.Command = cluster.Command?.Trim() ?? string.Empty;
        cluster.QueueLength = cluster.QueueLength <= 0 ? 500 : cluster.QueueLength;
    }
}
