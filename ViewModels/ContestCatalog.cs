namespace HamBusLog.ViewModels;

public static class ContestFieldKeys
{
    public const string Exchange = "exchange";
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
    bool UsesFieldDayExchange,
    DateTime? StartUtc,
    DateTime? EndUtc);

public static class ContestDefinitionExtensions
{
    public static ContestDefinition Clone(this ContestDefinition contest)
        => new(
            contest.Key,
            contest.DisplayName,
            contest.AdifContestId,
            contest.RequiredFields.Select(field => new ContestFieldRequirement(field.Key, field.Label, field.DetailFieldName)).ToList(),
            contest.UsesNormalExchange,
            contest.UsesFieldDayExchange,
            contest.StartUtc,
            contest.EndUtc);
}

public static class ContestCatalog
{
    public const string NormalKey = "NORMAL";
    public const string ArrlFieldDayKey = "ARRL-FD";
    public const string ArrlFieldDayAdifId = "ARRL-FIELD-DAY";

    private static string ToContestKey(ContestType type)
        => type == ContestType.ArrlFieldDay ? ArrlFieldDayKey : NormalKey;

    public static ContestDefinition Get(ContestType type)
    {
        var key = ToContestKey(type);
        return (GetByKey(key) ?? BuildBuiltIn(type)).Clone();
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
            .Select(x => x.Clone())
            .ToList();

        if (contests.Count == 0)
            return [BuildBuiltIn(ContestType.Normal).Clone()];

        return contests;
    }

    private static ContestDefinition ToDefinition(ContestDefinitionConfig config)
    {
        var key = string.IsNullOrWhiteSpace(config.Key) ? config.AdifContestId.Trim() : config.Key.Trim();
        var displayName = string.IsNullOrWhiteSpace(config.DisplayName) ? key : config.DisplayName.Trim();
        var adifId = string.IsNullOrWhiteSpace(config.AdifContestId) ? key : config.AdifContestId.Trim();
        var exchangeType = string.IsNullOrWhiteSpace(config.ExchangeType) ? "normal" : config.ExchangeType.Trim().ToLowerInvariant();
        var startUtc = ParseUtc(config.StartUtc);
        var endUtc = ParseUtc(config.EndUtc);

        if (string.Equals(exchangeType, "fieldday", StringComparison.OrdinalIgnoreCase))
            adifId = ArrlFieldDayAdifId;

        var requiredFields = config.RequiredFields
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(x => new ContestFieldRequirement(
                x.Key.Trim(),
                string.IsNullOrWhiteSpace(x.Label) ? x.Key.Trim() : x.Label.Trim(),
                string.IsNullOrWhiteSpace(x.DetailFieldName) ? null : x.DetailFieldName.Trim()))
            .ToList();

        return BuildFallback(key, displayName, exchangeType, requiredFields, adifId, startUtc, endUtc);
    }

    private static ContestDefinition BuildFallback(
        string key,
        string displayName,
        string exchangeType,
        IReadOnlyList<ContestFieldRequirement> requiredFields,
        string? adifId = null,
        DateTime? startUtc = null,
        DateTime? endUtc = null)
    {
        return new ContestDefinition(
            key,
            displayName,
            string.IsNullOrWhiteSpace(adifId) ? key : adifId,
            requiredFields,
            UsesNormalExchange: exchangeType == "normal",
            UsesFieldDayExchange: exchangeType == "fieldday",
            StartUtc: startUtc,
            EndUtc: endUtc);
    }

    private static ContestDefinition BuildBuiltIn(ContestType type)
    {
        return type == ContestType.ArrlFieldDay
            ? new ContestDefinition(
                ArrlFieldDayKey,
                "ARRL Field Day",
                ArrlFieldDayAdifId,
                [
                    new ContestFieldRequirement(ContestFieldKeys.FieldDaySection, "Field Day Section", "Section"),
                    new ContestFieldRequirement(ContestFieldKeys.FieldDayClass, "Field Day Class", "Class")
                ],
                UsesNormalExchange: false,
                UsesFieldDayExchange: true,
                StartUtc: null,
                EndUtc: null)
            : new ContestDefinition(
                NormalKey,
                "Normal",
                NormalKey,
                [],
                UsesNormalExchange: true,
                UsesFieldDayExchange: false,
                StartUtc: null,
                EndUtc: null);
    }

    private static DateTime? ParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd HHmm", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return parsed;

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            return parsed;

        return null;
    }
}



