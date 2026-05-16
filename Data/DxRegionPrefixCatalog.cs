namespace HamBusLog.Data;

public sealed record DxRegionPrefixDefinition(string Region, IReadOnlyList<string> Prefixes);

public static class DxRegionPrefixCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly string UserConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "hambuslog",
        "dx-region-prefixes.json");

    private static readonly Lazy<IReadOnlyDictionary<SpotRegion, IReadOnlyList<string>>> Cached
        = new(LoadPrefixMap);

    public static IReadOnlyDictionary<SpotRegion, IReadOnlyList<string>> GetPrefixes()
        => Cached.Value;

    public static string GetUserConfigPath() => UserConfigPath;

    public static bool EnsureUserConfigExists(out string path, out string errorMessage)
    {
        path = UserConfigPath;
        errorMessage = string.Empty;

        try
        {
            var directory = Path.GetDirectoryName(UserConfigPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(UserConfigPath))
                return true;

            var bundled = TryReadBundledConfig();
            if (string.IsNullOrWhiteSpace(bundled))
            {
                errorMessage = "Bundled prefix config is missing.";
                return false;
            }

            File.WriteAllText(UserConfigPath, bundled);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static IReadOnlyDictionary<SpotRegion, IReadOnlyList<string>> LoadPrefixMap()
    {
        var config = LoadConfig();
        var map = new Dictionary<SpotRegion, IReadOnlyList<string>>();

        foreach (var entry in config.Regions)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Region))
                continue;

            if (!TryParseRegion(entry.Region, out var region) || region == SpotRegion.All)
                continue;

            var prefixes = entry.Prefixes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (prefixes.Count == 0)
                continue;

            map[region] = prefixes;
        }

        return map.Count == 0 ? BuildFallbackMap() : map;
    }

    private static DxRegionPrefixConfig LoadConfig()
    {
        var json = TryReadUserConfig() ?? TryReadBundledConfig();
        if (string.IsNullOrWhiteSpace(json))
            return new DxRegionPrefixConfig();

        try
        {
            return JsonSerializer.Deserialize<DxRegionPrefixConfig>(json, JsonOptions)
                   ?? new DxRegionPrefixConfig();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DX region prefix catalog load error: {ex}");
            return new DxRegionPrefixConfig();
        }
    }

    private static string? TryReadUserConfig()
    {
        try
        {
            if (!File.Exists(UserConfigPath))
                return null;

            return File.ReadAllText(UserConfigPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DX region prefix user config read error: {ex}");
            return null;
        }
    }

    private static string? TryReadBundledConfig()
    {
        try
        {
            var bundledPath = Path.Combine(AppContext.BaseDirectory, "dx-region-prefixes.json");
            if (!File.Exists(bundledPath))
                return null;

            return File.ReadAllText(bundledPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DX region prefix bundled config read error: {ex}");
            return null;
        }
    }

    private static bool TryParseRegion(string value, out SpotRegion region)
    {
        var upper = value.Trim().ToUpperInvariant();
        region = upper switch
        {
            "NA" => SpotRegion.NorthAmerica,
            "SA" => SpotRegion.SouthAmerica,
            "EU" => SpotRegion.Europe,
            "AF" => SpotRegion.Africa,
            "AS" => SpotRegion.Asia,
            "OC" => SpotRegion.Oceania,
            "AN" => SpotRegion.Antarctica,
            "ALL" => SpotRegion.All,
            _ => SpotRegion.Unknown
        };

        return region != SpotRegion.Unknown;
    }

    private static IReadOnlyDictionary<SpotRegion, IReadOnlyList<string>> BuildFallbackMap()
    {
        return new Dictionary<SpotRegion, IReadOnlyList<string>>
        {
            [SpotRegion.NorthAmerica] =
            [
                "K", "W", "N", "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL",
                "KA", "KB", "KC", "KD", "KE", "KF", "KG", "KH", "KI", "KJ", "KK", "KL", "KM", "KN",
                "KO", "KP", "KQ", "KR", "KS", "KT", "KU", "KV", "KW", "KX", "KY", "KZ",
                "VE", "VA", "VO", "VY", "XE", "XF", "XJ", "XL", "XM", "XN", "XO", "XP", "XQ", "XR",
                "CY", "CZ", "KP2", "KP4", "KP5", "KG4", "KH6", "KH7", "KH8"
            ],
            [SpotRegion.SouthAmerica] =
            [
                "LU", "CX", "PY", "ZP", "CE", "OA", "YV", "HK", "HC", "HP", "TI"
            ],
            [SpotRegion.Europe] =
            [
                "G", "GM", "GW", "GI", "GD", "GJ", "GU", "EI", "F", "DL", "PA", "ON", "LX", "HB",
                "I", "IS", "TF", "EA", "CT", "OK", "OM", "SP", "SM", "OH", "LA", "OZ", "OE", "9A",
                "S5", "LZ", "YO", "SV", "SV9", "ER", "UR", "UA", "LY", "YL", "ES", "HA", "YU", "TK"
            ],
            [SpotRegion.Africa] =
            [
                "ZS", "5H", "5R", "9J", "9X", "7P", "C5", "CN", "5N", "5X", "3V", "ET", "A5"
            ],
            [SpotRegion.Asia] =
            [
                "JA", "JR", "JS", "JE", "JG", "JD", "7J", "7K", "7L", "7M", "7N", "8J", "8N",
                "BY", "BA", "BG", "BH", "BV", "VR", "HL", "DS", "HS", "9M2", "9M6", "9M8", "DU",
                "YB", "YC", "YD", "YE", "A6", "A7", "A9", "VU", "9V", "9W", "4X", "4Z"
            ],
            [SpotRegion.Oceania] =
            [
                "VK", "ZL", "VK0", "VK9", "FK", "FO", "KH0", "KH2", "KH1", "KH3"
            ],
            [SpotRegion.Antarctica] =
            [
                "VP8", "3Y", "FT", "LZ0", "RI1", "KC4"
            ]
        };
    }
}

public sealed class DxRegionPrefixConfig
{
    public List<DxRegionPrefixDefinition> Regions { get; set; } = [];
}


