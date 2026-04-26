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
}
