namespace HamBusLog.Data;

public static class ArrlFieldDaySectionCatalog
{
    private static readonly IReadOnlyList<string> Sections =
    [
        "AB", "AK", "AL", "AR", "AZ", "BC", "CO", "CT", "DC", "DE", "EB", "EMA", "ENY", "EPA", "EWA",
        "GA", "GH", "IA", "ID", "IL", "IN", "KS", "KY", "LA", "LAX", "MAR", "MB", "MDC", "ME", "MI",
        "MN", "MO", "MS", "MT", "NB", "NC", "ND", "NE", "NFL", "NH", "NL", "NLI", "NM", "NNJ", "NNY",
        "NS", "NT", "NTX", "NV", "NY", "OH", "OK", "ON", "ONE", "ONN", "ONS", "OR", "ORG", "PAC", "PE",
        "PR", "QC", "RI", "SB", "SC", "SCV", "SD", "SDG", "SF", "SFL", "SJV", "SK", "SNJ", "STX", "SV",
        "TER", "TN", "UT", "VA", "VI", "VT", "WCF", "WI", "WMA", "WNY", "WPA", "WTX", "WV", "WWA", "WY",
        "DX"
    ];

    public static IReadOnlyList<string> GetAll() => Sections;

    public static ISet<string> GetSet()
        => Sections.ToHashSet(StringComparer.OrdinalIgnoreCase);
}


