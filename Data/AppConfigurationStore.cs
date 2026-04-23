using System;
using System.IO;
using System.Text.Json;
using HamBusLog.Models;

namespace HamBusLog.Data;

public static class AppConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HamBusLog",
        "appsettings.json");

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
            return config ?? new AppConfiguration();
        }
        catch
        {
            return new AppConfiguration();
        }
    }

    public static void Save(AppConfiguration configuration)
    {
        var directory = Path.GetDirectoryName(ConfigFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }
}

