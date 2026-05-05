namespace HamBusLog.ViewModels;

public static class ContestFieldKeys
{
    public const string RstSent = "rst_sent";
    public const string RstRecv = "rst_recv";
    public const string Country = "country";
    public const string Name = "name";
    public const string State = "state";
    public const string County = "county";
    public const string FieldDaySection = "fd_section";
    public const string FieldDayClass = "fd_class";
}

public sealed record ContestFieldRequirement(string Key, string Label, string? DetailFieldName = null);

public sealed record ContestDefinition(
    string Key,
    string DisplayName,
    string AdifContestId,
    IReadOnlyList<ContestFieldRequirement> RequiredFields,
    bool UsesNormalExchange,
    bool UsesFieldDayExchange);

public static class ContestCatalog
{
    public const string NormalKey = "NORMAL";
    public const string ArrlFieldDayKey = "ARRL-FD";

    private static string ToContestKey(ContestType type)
        => type == ContestType.ArrlFieldDay ? ArrlFieldDayKey : NormalKey;

    public static ContestDefinition Get(ContestType type)
    {
        var key = ToContestKey(type);
        return GetByKey(key)
               ?? BuildFallback(key, key, type == ContestType.ArrlFieldDay ? "fieldday" : "normal", []);
    }

    public static ContestDefinition? GetByKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return GetAll().FirstOrDefault(x => string.Equals(x.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<ContestDefinition> GetAll()
    {
        var config = AppConfigurationStore.Load();
        var contests = config.Contests
            .Where(x => x is not null)
            .Select(ToDefinition)
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToList();

        if (contests.Count == 0)
            return [BuildFallback(NormalKey, "Normal", "normal", [])];

        return contests;
    }

    private static ContestDefinition ToDefinition(ContestDefinitionConfig config)
    {
        var key = string.IsNullOrWhiteSpace(config.Key) ? config.AdifContestId.Trim() : config.Key.Trim();
        var displayName = string.IsNullOrWhiteSpace(config.DisplayName) ? key : config.DisplayName.Trim();
        var adifId = string.IsNullOrWhiteSpace(config.AdifContestId) ? key : config.AdifContestId.Trim();
        var exchangeType = string.IsNullOrWhiteSpace(config.ExchangeType) ? "normal" : config.ExchangeType.Trim().ToLowerInvariant();

        var requiredFields = config.RequiredFields
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(x => new ContestFieldRequirement(
                x.Key.Trim(),
                string.IsNullOrWhiteSpace(x.Label) ? x.Key.Trim() : x.Label.Trim(),
                string.IsNullOrWhiteSpace(x.DetailFieldName) ? null : x.DetailFieldName.Trim()))
            .ToList();

        return BuildFallback(key, displayName, exchangeType, requiredFields, adifId);
    }

    private static ContestDefinition BuildFallback(string key, string displayName, string exchangeType, IReadOnlyList<ContestFieldRequirement> requiredFields, string? adifId = null)
    {
        return new ContestDefinition(
            key,
            displayName,
            string.IsNullOrWhiteSpace(adifId) ? key : adifId,
            requiredFields,
            UsesNormalExchange: exchangeType == "normal",
            UsesFieldDayExchange: exchangeType == "fieldday");
    }
}



