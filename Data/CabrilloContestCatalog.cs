namespace HamBusLog.Data;

public sealed record CabrilloHeaderFieldDefinition(
    string Key,
    string Label,
    string? DefaultSource,
    string? DefaultValue,
    bool IsRequired,
    bool IsUppercase,
    bool IsMultiline,
    string InputType,
    IReadOnlyList<string> Options);

public sealed record CabrilloContestDefinition(
    string Key,
    string DisplayName,
    string AdifContestId,
    IReadOnlyList<string> AdifContestIds,
    string ExporterKey,
    IReadOnlyList<CabrilloHeaderFieldDefinition> HeaderFields);

public static class CabrilloContestCatalog
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
        "cabrillo-contests.json");

    public static IReadOnlyList<CabrilloContestDefinition> GetAll()
    {
        var config = LoadConfig();
        if (config.Contests.Count == 0)
            return BuildFallbackContests();

        var normalized = new List<CabrilloContestDefinition>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var contest in config.Contests)
        {
            if (contest is null)
                continue;

            var key = NormalizeKey(contest.Key, contest.AdifContestId);
            if (string.IsNullOrWhiteSpace(key) || !seenKeys.Add(key))
                continue;

            var displayName = string.IsNullOrWhiteSpace(contest.DisplayName) ? key : contest.DisplayName.Trim();
            var adifId = string.IsNullOrWhiteSpace(contest.AdifContestId) ? key : contest.AdifContestId.Trim();
            var exporterKey = string.IsNullOrWhiteSpace(contest.ExporterKey) ? key : contest.ExporterKey.Trim();
            var ids = NormalizeContestIds(adifId, contest.AdifContestIds);

            var headers = contest.HeaderFields
                .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Key))
                .Select(x => new CabrilloHeaderFieldDefinition(
                    x.Key.Trim(),
                    string.IsNullOrWhiteSpace(x.Label) ? x.Key.Trim() : x.Label.Trim(),
                    string.IsNullOrWhiteSpace(x.DefaultSource) ? null : x.DefaultSource.Trim(),
                    string.IsNullOrWhiteSpace(x.DefaultValue) ? null : x.DefaultValue.Trim(),
                    x.IsRequired,
                    x.IsUppercase,
                    x.IsMultiline,
                    string.IsNullOrWhiteSpace(x.InputType) ? "text" : x.InputType.Trim(),
                    x.Options
                        .Where(option => !string.IsNullOrWhiteSpace(option))
                        .Select(option => option.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()))
                .ToList();

            ApplyKnownHeaderDefaults(key, adifId, exporterKey, headers);

            normalized.Add(new CabrilloContestDefinition(
                key,
                displayName,
                adifId,
                ids,
                exporterKey,
                headers));
        }

        return normalized.Count == 0 ? BuildFallbackContests() : normalized;
    }

    public static CabrilloContestDefinition? GetByKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return GetAll().FirstOrDefault(x => string.Equals(x.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeKey(string? key, string? adifId)
    {
        if (!string.IsNullOrWhiteSpace(key))
            return key.Trim();

        return string.IsNullOrWhiteSpace(adifId) ? string.Empty : adifId.Trim();
    }

    private static IReadOnlyList<string> NormalizeContestIds(string adifId, IReadOnlyList<string>? ids)
    {
        var all = new List<string>();
        if (!string.IsNullOrWhiteSpace(adifId))
            all.Add(adifId.Trim());

        if (ids is not null)
        {
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var trimmed = id.Trim();
                if (!all.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    all.Add(trimmed);
            }
        }

        return all;
    }

    private static CabrilloContestCatalogConfig LoadConfig()
    {
        var json = TryReadUserConfig() ?? TryReadBundledConfig();
        if (string.IsNullOrWhiteSpace(json))
            return new CabrilloContestCatalogConfig();

        try
        {
            return JsonSerializer.Deserialize<CabrilloContestCatalogConfig>(json, JsonOptions)
                   ?? new CabrilloContestCatalogConfig();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cabrillo contest catalog load error: {ex}");
            return new CabrilloContestCatalogConfig();
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
            System.Diagnostics.Debug.WriteLine($"Cabrillo contest catalog user config read error: {ex}");
            return null;
        }
    }

    private static string? TryReadBundledConfig()
    {
        try
        {
            var bundledPath = Path.Combine(AppContext.BaseDirectory, "cabrillo-contests.json");
            if (!File.Exists(bundledPath))
                return null;

            return File.ReadAllText(bundledPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cabrillo contest catalog bundled config read error: {ex}");
            return null;
        }
    }

    private static IReadOnlyList<CabrilloContestDefinition> BuildFallbackContests()
    {
        return
        [
            new CabrilloContestDefinition(
                "ARQP",
                "AR-QSO-PARTY Arkansas QSO Party",
                "AR-QSO-PARTY",
                ["AR-QSO-PARTY", "ARQP"],
                "ARQP",
                []),
            new CabrilloContestDefinition(
                "ARRL-FD",
                "ARRL Field Day",
                "ARRL-FIELD-DAY",
                ["ARRL-FIELD-DAY", "ARRL-FD"],
                "ARRL-FD",
                [])
        ];
    }

    private static void ApplyKnownHeaderDefaults(
        string key,
        string adifId,
        string exporterKey,
        List<CabrilloHeaderFieldDefinition> headers)
    {
        var upperKey = key.Trim().ToUpperInvariant();
        var upperAdif = adifId.Trim().ToUpperInvariant();
        var upperExporter = exporterKey.Trim().ToUpperInvariant();

        if (upperKey is "ARQP" or "AR-QSO-PARTY"
            || upperAdif is "ARQP" or "AR-QSO-PARTY"
            || upperExporter == "ARQP")
        {
            EnsureSelectField(headers, "CATEGORY-POWER", "Category Power", ["QRP", "LOW", "HIGH"]);
            EnsureSelectField(headers, "CATEGORY-MODE", "Category Mode", ["CW", "SSB", "FM", "RTTY", "DIGITAL", "MIXED"]);
            EnsureTextField(headers, "CATEGORY-CLASS", "Category Class", isUppercase: true);
            EnsureComputedField(headers, "CLAIMED-SCORE", "Claimed Score");
        }

        if (upperKey is "ARRL-FD" or "ARRL-FIELD-DAY"
            || upperAdif is "ARRL-FD" or "ARRL-FIELD-DAY"
            || upperExporter == "ARRL-FD")
        {
            EnsureComputedField(headers, "CLAIMED-SCORE", "Claimed Score");
        }
    }

    private static void EnsureSelectField(
        List<CabrilloHeaderFieldDefinition> headers,
        string key,
        string label,
        IReadOnlyList<string> options)
    {
        var index = FindHeaderIndex(headers, key);
        if (index >= 0)
        {
            var current = headers[index];
            headers[index] = current with
            {
                InputType = "select",
                Options = options.ToList()
            };
            return;
        }

        headers.Add(new CabrilloHeaderFieldDefinition(
            key,
            label,
            null,
            null,
            false,
            true,
            false,
            "select",
            options.ToList()));
    }

    private static void EnsureComputedField(List<CabrilloHeaderFieldDefinition> headers, string key, string label)
    {
        var index = FindHeaderIndex(headers, key);
        if (index >= 0)
        {
            var current = headers[index];
            headers[index] = current with { InputType = "computed" };
            return;
        }

        headers.Add(new CabrilloHeaderFieldDefinition(
            key,
            label,
            null,
            null,
            false,
            false,
            false,
            "computed",
            []));
    }

    private static void EnsureTextField(List<CabrilloHeaderFieldDefinition> headers, string key, string label, bool isUppercase)
    {
        if (FindHeaderIndex(headers, key) >= 0)
            return;

        headers.Add(new CabrilloHeaderFieldDefinition(
            key,
            label,
            null,
            null,
            false,
            isUppercase,
            false,
            "text",
            []));
    }

    private static int FindHeaderIndex(List<CabrilloHeaderFieldDefinition> headers, string key)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i].Key, key, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}

public sealed class CabrilloContestCatalogConfig
{
    public List<CabrilloContestDefinitionConfig> Contests { get; set; } = [];
}

public sealed class CabrilloContestDefinitionConfig
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AdifContestId { get; set; } = string.Empty;
    public List<string> AdifContestIds { get; set; } = [];
    public string ExporterKey { get; set; } = string.Empty;
    public List<CabrilloHeaderFieldDefinitionConfig> HeaderFields { get; set; } = [];
}

public sealed class CabrilloHeaderFieldDefinitionConfig
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DefaultSource { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsUppercase { get; set; }
    public bool IsMultiline { get; set; }
    public string InputType { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
}



